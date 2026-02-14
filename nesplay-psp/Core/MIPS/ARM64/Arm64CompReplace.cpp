// Copyright (c) 2013- NESPLAY_PSP Project.

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 2.0 or later versions.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License 2.0 for more details.

// A copy of the GPL 2.0 should have been included with the program.
// If not, see http://www.gnu.org/licenses/

// Official git repository and contact information can be found at
// https://github.com/hrydgard/nesplay_psp and http://www.nesplay_psp.org/.

#include "nesplay_psp_config.h"
#if NESPLAY_PSP_ARCH(ARM64)

#include "Common/CPUDetect.h"
#include "Core/MemMap.h"
#include "Core/MIPS/JitCommon/JitCommon.h"
#include "Core/MIPS/ARM64/Arm64Jit.h"
#include "Core/MIPS/ARM64/Arm64RegCache.h"

namespace MIPSComp {

int Arm64Jit::Replace_fabsf() {
	fpr.MapDirtyIn(0, 12);
	fp.FABS(fpr.R(0), fpr.R(12));
	return 4;  // Number of instructions in the MIPS function
}

}

#endif // NESPLAY_PSP_ARCH(ARM64)
