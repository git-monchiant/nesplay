using Raylib_cs;
using System.Runtime.InteropServices;

public class APU {
    private Bus bus;

    // Audio output
    private const int SAMPLE_RATE = 44100;
    private const int STREAM_BUFFER = 2048;
    private AudioStream audioStream;

    // Ring buffer to decouple generation from playback
    private const int RING_SIZE = 16384;
    private short[] ringBuffer = new short[RING_SIZE];
    private int ringWrite = 0;
    private int ringRead = 0;
    private short[] pushBuffer = new short[STREAM_BUFFER];

    // Timing
    private const double CPU_FREQ = 1789773.0;
    private double sampleTimer = 0;
    private double samplePeriod = CPU_FREQ / SAMPLE_RATE;
    private bool apuCycleToggle = false;

    // High-pass filter to remove DC offset
    private float hpPrevInput = 0;
    private float hpPrevOutput = 0;
    private const float HP_ALPHA = 0.995f;

    // Low-pass filter to reduce aliasing/harshness
    private float lpPrevOutput = 0;
    private const float LP_ALPHA = 0.65f; // ~14kHz cutoff

    // Frame counter
    private int frameCounterTimer = 0;
    private bool frameCounterMode = false; // false=4-step, true=5-step
    private bool frameIrqInhibit = false;
    private bool frameIrqFlag = false;

    // Length counter lookup table
    private static readonly byte[] lengthTable = {
        10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14,
        12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30
    };

    // Duty cycle sequences for pulse channels
    private static readonly byte[,] dutyTable = {
        { 0, 0, 0, 0, 0, 0, 0, 1 }, // 12.5%
        { 0, 0, 0, 0, 0, 0, 1, 1 }, // 25%
        { 0, 0, 0, 0, 1, 1, 1, 1 }, // 50%
        { 1, 1, 1, 1, 1, 1, 0, 0 }, // 75%
    };

    // Triangle wave sequence
    private static readonly byte[] triangleTable = {
        15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15
    };

    // Noise period table (NTSC)
    private static readonly ushort[] noisePeriodTable = {
        4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068
    };

    // DMC rate table (NTSC)
    private static readonly ushort[] dmcRateTable = {
        428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54
    };

    // === Pulse Channel 1 ===
    private int pulse1Duty, pulse1DutyPos;
    private bool pulse1LengthHalt, pulse1ConstantVol;
    private int pulse1Volume, pulse1EnvelopeTimer, pulse1EnvelopeDecay;
    private bool pulse1EnvelopeStart;
    private int pulse1SweepPeriod, pulse1SweepShift;
    private bool pulse1SweepEnabled, pulse1SweepNegate, pulse1SweepReload;
    private int pulse1SweepTimer;
    private int pulse1Timer, pulse1TimerPeriod;
    private int pulse1LengthCounter;
    private bool pulse1Enabled;

    // === Pulse Channel 2 ===
    private int pulse2Duty, pulse2DutyPos;
    private bool pulse2LengthHalt, pulse2ConstantVol;
    private int pulse2Volume, pulse2EnvelopeTimer, pulse2EnvelopeDecay;
    private bool pulse2EnvelopeStart;
    private int pulse2SweepPeriod, pulse2SweepShift;
    private bool pulse2SweepEnabled, pulse2SweepNegate, pulse2SweepReload;
    private int pulse2SweepTimer;
    private int pulse2Timer, pulse2TimerPeriod;
    private int pulse2LengthCounter;
    private bool pulse2Enabled;

    // === Triangle Channel ===
    private bool triLengthHalt;
    private int triLinearLoad, triLinearCounter;
    private bool triLinearReload;
    private int triTimer, triTimerPeriod;
    private int triLengthCounter;
    private int triSequencePos;
    private bool triEnabled;

    // === Noise Channel ===
    private bool noiseLengthHalt, noiseConstantVol;
    private int noiseVolume, noiseEnvelopeTimer, noiseEnvelopeDecay;
    private bool noiseEnvelopeStart;
    private bool noiseMode;
    private int noiseTimer, noisePeriod;
    private int noiseLengthCounter;
    private int noiseShiftReg = 1;
    private bool noiseEnabled;

    // === DMC Channel ===
    private bool dmcIrqEnabled, dmcLoop;
    private int dmcRate, dmcTimer;
    private int dmcOutputLevel;
    private int dmcSampleAddr, dmcSampleLength;
    private int dmcCurrentAddr, dmcBytesRemaining;
    private int dmcSampleBuffer;
    private bool dmcSampleBufferEmpty = true;
    private int dmcShiftReg, dmcBitsRemaining;
    private bool dmcSilence = true;
    private bool dmcEnabled;

    public APU(Bus bus) {
        this.bus = bus;
        Raylib.SetAudioStreamBufferSizeDefault(STREAM_BUFFER);
        audioStream = Raylib.LoadAudioStream(SAMPLE_RATE, 16, 1);
        Raylib.PlayAudioStream(audioStream);
        Raylib.SetAudioStreamVolume(audioStream, 0.5f);
    }

    public void Step(int cpuCycles) {
        for (int i = 0; i < cpuCycles; i++) {
            // Frame counter (runs at CPU clock)
            ClockFrameCounter();

            // Pulse/Noise/DMC timers clocked every 2 CPU cycles
            apuCycleToggle = !apuCycleToggle;
            if (apuCycleToggle) {
                ClockPulse1Timer();
                ClockPulse2Timer();
                ClockNoiseTimer();
                ClockDMC();
            }

            // Triangle timer (clocked every CPU cycle)
            ClockTriangleTimer();

            // Sample generation
            sampleTimer++;
            if (sampleTimer >= samplePeriod) {
                sampleTimer -= samplePeriod;
                GenerateSample();
            }
        }
    }

    public void OutputSamples() {
        while (Raylib.IsAudioStreamProcessed(audioStream)) {
            int available = (ringWrite - ringRead + RING_SIZE) % RING_SIZE;
            if (available == 0) break;

            // Copy what we have, pad remainder with last sample to avoid pops
            int toCopy = Math.Min(available, STREAM_BUFFER);
            short lastSample = 0;
            for (int i = 0; i < toCopy; i++) {
                lastSample = ringBuffer[ringRead];
                pushBuffer[i] = lastSample;
                ringRead = (ringRead + 1) % RING_SIZE;
            }
            for (int i = toCopy; i < STREAM_BUFFER; i++) {
                pushBuffer[i] = lastSample; // pad with last value, not zero
            }

            unsafe {
                fixed (short* ptr = pushBuffer) {
                    Raylib.UpdateAudioStream(audioStream, ptr, STREAM_BUFFER);
                }
            }
        }
    }

    private void GenerateSample() {
        int nextWrite = (ringWrite + 1) % RING_SIZE;
        if (nextWrite == ringRead) return; // ring full, drop sample

        float pulse1 = GetPulse1Output();
        float pulse2 = GetPulse2Output();
        float triangle = GetTriangleOutput();
        float noise = GetNoiseOutput();
        float dmc = GetDMCOutput();

        // NES mixer formula (approximation)
        float pulseOut = 0;
        if (pulse1 + pulse2 > 0)
            pulseOut = 95.88f / (8128.0f / (pulse1 + pulse2) + 100.0f);

        float tndOut = 0;
        float tndSum = triangle / 8227.0f + noise / 12241.0f + dmc / 22638.0f;
        if (tndSum > 0)
            tndOut = 159.79f / (1.0f / tndSum + 100.0f);

        float output = pulseOut + tndOut;

        // High-pass filter to remove DC offset
        float filtered = HP_ALPHA * (hpPrevOutput + output - hpPrevInput);
        hpPrevInput = output;
        hpPrevOutput = filtered;

        // Low-pass filter to smooth harsh edges
        float smoothed = lpPrevOutput + LP_ALPHA * (filtered - lpPrevOutput);
        lpPrevOutput = smoothed;

        // Clamp to short range
        int sampleVal = (int)(smoothed * 22000);
        if (sampleVal > 32767) sampleVal = 32767;
        if (sampleVal < -32768) sampleVal = -32768;

        ringBuffer[ringWrite] = (short)sampleVal;
        ringWrite = nextWrite;
    }

    // === Pulse 1 Output ===
    private float GetPulse1Output() {
        if (!pulse1Enabled || pulse1LengthCounter == 0) return 0;
        if (pulse1TimerPeriod < 8 || pulse1TimerPeriod > 0x7FF) return 0;
        if (dutyTable[pulse1Duty, pulse1DutyPos] == 0) return 0;
        return pulse1ConstantVol ? pulse1Volume : pulse1EnvelopeDecay;
    }

    // === Pulse 2 Output ===
    private float GetPulse2Output() {
        if (!pulse2Enabled || pulse2LengthCounter == 0) return 0;
        if (pulse2TimerPeriod < 8 || pulse2TimerPeriod > 0x7FF) return 0;
        if (dutyTable[pulse2Duty, pulse2DutyPos] == 0) return 0;
        return pulse2ConstantVol ? pulse2Volume : pulse2EnvelopeDecay;
    }

    // === Triangle Output ===
    private float GetTriangleOutput() {
        if (!triEnabled || triLengthCounter == 0 || triLinearCounter == 0) return 0;
        if (triTimerPeriod < 2) return 0;
        return triangleTable[triSequencePos];
    }

    // === Noise Output ===
    private float GetNoiseOutput() {
        if (!noiseEnabled || noiseLengthCounter == 0) return 0;
        if ((noiseShiftReg & 1) != 0) return 0;
        return noiseConstantVol ? noiseVolume : noiseEnvelopeDecay;
    }

    // === DMC Output ===
    private float GetDMCOutput() {
        return dmcOutputLevel;
    }

    // === Timer Clocking ===
    private void ClockPulse1Timer() {
        if (pulse1Timer > 0) {
            pulse1Timer--;
        } else {
            pulse1Timer = pulse1TimerPeriod;
            pulse1DutyPos = (pulse1DutyPos + 1) & 7;
        }
    }

    private void ClockPulse2Timer() {
        if (pulse2Timer > 0) {
            pulse2Timer--;
        } else {
            pulse2Timer = pulse2TimerPeriod;
            pulse2DutyPos = (pulse2DutyPos + 1) & 7;
        }
    }

    private void ClockTriangleTimer() {
        if (triTimer > 0) {
            triTimer--;
        } else {
            triTimer = triTimerPeriod;
            if (triLengthCounter > 0 && triLinearCounter > 0) {
                triSequencePos = (triSequencePos + 1) & 31;
            }
        }
    }

    private void ClockNoiseTimer() {
        if (noiseTimer > 0) {
            noiseTimer--;
        } else {
            noiseTimer = noisePeriod;
            int bit0 = noiseShiftReg & 1;
            int other = noiseMode ? (noiseShiftReg >> 6) & 1 : (noiseShiftReg >> 1) & 1;
            int feedback = bit0 ^ other;
            noiseShiftReg >>= 1;
            noiseShiftReg |= feedback << 14;
        }
    }

    private void ClockDMC() {
        if (dmcTimer > 0) {
            dmcTimer--;
            return;
        }
        dmcTimer = dmcRate;

        if (!dmcSilence) {
            if ((dmcShiftReg & 1) != 0) {
                if (dmcOutputLevel <= 125) dmcOutputLevel += 2;
            } else {
                if (dmcOutputLevel >= 2) dmcOutputLevel -= 2;
            }
            dmcShiftReg >>= 1;
        }

        dmcBitsRemaining--;
        if (dmcBitsRemaining <= 0) {
            dmcBitsRemaining = 8;
            if (dmcSampleBufferEmpty) {
                dmcSilence = true;
            } else {
                dmcSilence = false;
                dmcShiftReg = dmcSampleBuffer;
                dmcSampleBufferEmpty = true;
                FetchDMCSample();
            }
        }
    }

    private void FetchDMCSample() {
        if (dmcBytesRemaining > 0) {
            dmcSampleBuffer = bus.Read((ushort)dmcCurrentAddr);
            dmcSampleBufferEmpty = false;
            dmcCurrentAddr = (dmcCurrentAddr + 1) | 0x8000;
            if (dmcCurrentAddr > 0xFFFF) dmcCurrentAddr = 0x8000;
            dmcBytesRemaining--;
            if (dmcBytesRemaining == 0) {
                if (dmcLoop) {
                    dmcCurrentAddr = dmcSampleAddr;
                    dmcBytesRemaining = dmcSampleLength;
                }
            }
        }
    }

    // === Frame Counter ===
    private void ClockFrameCounter() {
        frameCounterTimer++;

        if (!frameCounterMode) {
            // 4-step mode
            switch (frameCounterTimer) {
                case 7457: ClockEnvelopes(); ClockTriLinear(); break;
                case 14913: ClockEnvelopes(); ClockTriLinear(); ClockLengthCounters(); ClockSweeps(); break;
                case 22371: ClockEnvelopes(); ClockTriLinear(); break;
                case 29829:
                    ClockEnvelopes(); ClockTriLinear(); ClockLengthCounters(); ClockSweeps();
                    frameCounterTimer = 0;
                    break;
            }
        } else {
            // 5-step mode
            switch (frameCounterTimer) {
                case 7457: ClockEnvelopes(); ClockTriLinear(); break;
                case 14913: ClockEnvelopes(); ClockTriLinear(); ClockLengthCounters(); ClockSweeps(); break;
                case 22371: ClockEnvelopes(); ClockTriLinear(); break;
                case 29829: break; // nothing
                case 37281:
                    ClockEnvelopes(); ClockTriLinear(); ClockLengthCounters(); ClockSweeps();
                    frameCounterTimer = 0;
                    break;
            }
        }
    }

    private void ClockEnvelopes() {
        // Pulse 1
        if (pulse1EnvelopeStart) {
            pulse1EnvelopeStart = false;
            pulse1EnvelopeDecay = 15;
            pulse1EnvelopeTimer = pulse1Volume;
        } else {
            if (pulse1EnvelopeTimer > 0) {
                pulse1EnvelopeTimer--;
            } else {
                pulse1EnvelopeTimer = pulse1Volume;
                if (pulse1EnvelopeDecay > 0) pulse1EnvelopeDecay--;
                else if (pulse1LengthHalt) pulse1EnvelopeDecay = 15;
            }
        }

        // Pulse 2
        if (pulse2EnvelopeStart) {
            pulse2EnvelopeStart = false;
            pulse2EnvelopeDecay = 15;
            pulse2EnvelopeTimer = pulse2Volume;
        } else {
            if (pulse2EnvelopeTimer > 0) {
                pulse2EnvelopeTimer--;
            } else {
                pulse2EnvelopeTimer = pulse2Volume;
                if (pulse2EnvelopeDecay > 0) pulse2EnvelopeDecay--;
                else if (pulse2LengthHalt) pulse2EnvelopeDecay = 15;
            }
        }

        // Noise
        if (noiseEnvelopeStart) {
            noiseEnvelopeStart = false;
            noiseEnvelopeDecay = 15;
            noiseEnvelopeTimer = noiseVolume;
        } else {
            if (noiseEnvelopeTimer > 0) {
                noiseEnvelopeTimer--;
            } else {
                noiseEnvelopeTimer = noiseVolume;
                if (noiseEnvelopeDecay > 0) noiseEnvelopeDecay--;
                else if (noiseLengthHalt) noiseEnvelopeDecay = 15;
            }
        }
    }

    private void ClockTriLinear() {
        if (triLinearReload) {
            triLinearCounter = triLinearLoad;
        } else if (triLinearCounter > 0) {
            triLinearCounter--;
        }
        if (!triLengthHalt) triLinearReload = false;
    }

    private void ClockLengthCounters() {
        if (!pulse1LengthHalt && pulse1LengthCounter > 0) pulse1LengthCounter--;
        if (!pulse2LengthHalt && pulse2LengthCounter > 0) pulse2LengthCounter--;
        if (!triLengthHalt && triLengthCounter > 0) triLengthCounter--;
        if (!noiseLengthHalt && noiseLengthCounter > 0) noiseLengthCounter--;
    }

    private void ClockSweeps() {
        // Pulse 1 sweep
        if (pulse1SweepTimer == 0 && pulse1SweepEnabled && pulse1SweepShift > 0) {
            int change = pulse1TimerPeriod >> pulse1SweepShift;
            if (pulse1SweepNegate) change = -change - 1; // Pulse 1 uses one's complement
            int target = pulse1TimerPeriod + change;
            if (target >= 0 && target <= 0x7FF) {
                pulse1TimerPeriod = target;
            }
        }
        if (pulse1SweepTimer == 0 || pulse1SweepReload) {
            pulse1SweepTimer = pulse1SweepPeriod;
            pulse1SweepReload = false;
        } else {
            pulse1SweepTimer--;
        }

        // Pulse 2 sweep
        if (pulse2SweepTimer == 0 && pulse2SweepEnabled && pulse2SweepShift > 0) {
            int change = pulse2TimerPeriod >> pulse2SweepShift;
            if (pulse2SweepNegate) change = -change; // Pulse 2 uses two's complement
            int target = pulse2TimerPeriod + change;
            if (target >= 0 && target <= 0x7FF) {
                pulse2TimerPeriod = target;
            }
        }
        if (pulse2SweepTimer == 0 || pulse2SweepReload) {
            pulse2SweepTimer = pulse2SweepPeriod;
            pulse2SweepReload = false;
        } else {
            pulse2SweepTimer--;
        }
    }

    // === Register Read/Write ===
    public void WriteRegister(ushort address, byte value) {
        switch (address) {
            // Pulse 1
            case 0x4000:
                pulse1Duty = (value >> 6) & 3;
                pulse1LengthHalt = (value & 0x20) != 0;
                pulse1ConstantVol = (value & 0x10) != 0;
                pulse1Volume = value & 0x0F;
                break;
            case 0x4001:
                pulse1SweepEnabled = (value & 0x80) != 0;
                pulse1SweepPeriod = (value >> 4) & 7;
                pulse1SweepNegate = (value & 0x08) != 0;
                pulse1SweepShift = value & 7;
                pulse1SweepReload = true;
                break;
            case 0x4002:
                pulse1TimerPeriod = (pulse1TimerPeriod & 0x700) | value;
                break;
            case 0x4003:
                pulse1TimerPeriod = (pulse1TimerPeriod & 0xFF) | ((value & 7) << 8);
                if (pulse1Enabled) pulse1LengthCounter = lengthTable[(value >> 3) & 0x1F];
                pulse1EnvelopeStart = true;
                pulse1DutyPos = 0;
                break;

            // Pulse 2
            case 0x4004:
                pulse2Duty = (value >> 6) & 3;
                pulse2LengthHalt = (value & 0x20) != 0;
                pulse2ConstantVol = (value & 0x10) != 0;
                pulse2Volume = value & 0x0F;
                break;
            case 0x4005:
                pulse2SweepEnabled = (value & 0x80) != 0;
                pulse2SweepPeriod = (value >> 4) & 7;
                pulse2SweepNegate = (value & 0x08) != 0;
                pulse2SweepShift = value & 7;
                pulse2SweepReload = true;
                break;
            case 0x4006:
                pulse2TimerPeriod = (pulse2TimerPeriod & 0x700) | value;
                break;
            case 0x4007:
                pulse2TimerPeriod = (pulse2TimerPeriod & 0xFF) | ((value & 7) << 8);
                if (pulse2Enabled) pulse2LengthCounter = lengthTable[(value >> 3) & 0x1F];
                pulse2EnvelopeStart = true;
                pulse2DutyPos = 0;
                break;

            // Triangle
            case 0x4008:
                triLengthHalt = (value & 0x80) != 0;
                triLinearLoad = value & 0x7F;
                break;
            case 0x400A:
                triTimerPeriod = (triTimerPeriod & 0x700) | value;
                break;
            case 0x400B:
                triTimerPeriod = (triTimerPeriod & 0xFF) | ((value & 7) << 8);
                if (triEnabled) triLengthCounter = lengthTable[(value >> 3) & 0x1F];
                triLinearReload = true;
                break;

            // Noise
            case 0x400C:
                noiseLengthHalt = (value & 0x20) != 0;
                noiseConstantVol = (value & 0x10) != 0;
                noiseVolume = value & 0x0F;
                break;
            case 0x400E:
                noiseMode = (value & 0x80) != 0;
                noisePeriod = noisePeriodTable[value & 0x0F];
                break;
            case 0x400F:
                if (noiseEnabled) noiseLengthCounter = lengthTable[(value >> 3) & 0x1F];
                noiseEnvelopeStart = true;
                break;

            // DMC
            case 0x4010:
                dmcIrqEnabled = (value & 0x80) != 0;
                dmcLoop = (value & 0x40) != 0;
                dmcRate = dmcRateTable[value & 0x0F];
                break;
            case 0x4011:
                dmcOutputLevel = value & 0x7F;
                break;
            case 0x4012:
                dmcSampleAddr = 0xC000 + value * 64;
                break;
            case 0x4013:
                dmcSampleLength = value * 16 + 1;
                break;

            // Status
            case 0x4015:
                pulse1Enabled = (value & 1) != 0;
                pulse2Enabled = (value & 2) != 0;
                triEnabled = (value & 4) != 0;
                noiseEnabled = (value & 8) != 0;
                dmcEnabled = (value & 16) != 0;
                if (!pulse1Enabled) pulse1LengthCounter = 0;
                if (!pulse2Enabled) pulse2LengthCounter = 0;
                if (!triEnabled) triLengthCounter = 0;
                if (!noiseEnabled) noiseLengthCounter = 0;
                if (!dmcEnabled) {
                    dmcBytesRemaining = 0;
                } else if (dmcBytesRemaining == 0) {
                    dmcCurrentAddr = dmcSampleAddr;
                    dmcBytesRemaining = dmcSampleLength;
                    if (dmcSampleBufferEmpty) FetchDMCSample();
                }
                break;

            // Frame counter
            case 0x4017:
                frameCounterMode = (value & 0x80) != 0;
                frameIrqInhibit = (value & 0x40) != 0;
                if (frameCounterMode) {
                    ClockEnvelopes();
                    ClockTriLinear();
                    ClockLengthCounters();
                    ClockSweeps();
                }
                frameCounterTimer = 0;
                break;
        }
    }

    public byte ReadStatus() {
        byte status = 0;
        if (pulse1LengthCounter > 0) status |= 1;
        if (pulse2LengthCounter > 0) status |= 2;
        if (triLengthCounter > 0) status |= 4;
        if (noiseLengthCounter > 0) status |= 8;
        if (dmcBytesRemaining > 0) status |= 16;
        if (frameIrqFlag) status |= 0x40;
        frameIrqFlag = false;
        return status;
    }

    public void Close() {
        Raylib.UnloadAudioStream(audioStream);
    }
}
