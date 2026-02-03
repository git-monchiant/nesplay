using Mesen.Interop;
using Mesen.Utilities;
using System;

namespace Mesen.Config
{
	public static class KeyPresets
	{
		public static void ApplyWasdLayout(KeyMapping m, ControllerType type)
		{
			m.ClearKeys(type);

			m.A = InputApi.GetKeyCode("K");
			m.B = InputApi.GetKeyCode("J");
			m.Up = InputApi.GetKeyCode("W");
			m.Down = InputApi.GetKeyCode("S");
			m.Left = InputApi.GetKeyCode("A");
			m.Right = InputApi.GetKeyCode("D");

			if(type == ControllerType.SnesController || type == ControllerType.SnesRumbleController) {
				m.X = InputApi.GetKeyCode(";");
				m.Y = InputApi.GetKeyCode("M");
				m.L = InputApi.GetKeyCode("U");
				m.R = InputApi.GetKeyCode("I");
				m.Select = InputApi.GetKeyCode("O");
				m.Start = InputApi.GetKeyCode("L");
			} else if(type == ControllerType.PceAvenuePad6) {
				m.X = InputApi.GetKeyCode("H");
				m.Y = InputApi.GetKeyCode("Y");
				m.L = InputApi.GetKeyCode("U");
				m.R = InputApi.GetKeyCode("I");
				m.Select = InputApi.GetKeyCode("N");
				m.Start = InputApi.GetKeyCode("M");
			} else if(type == ControllerType.GbaController) {
				m.L = InputApi.GetKeyCode("U");
				m.R = InputApi.GetKeyCode("I");
				m.Select = InputApi.GetKeyCode("O");
				m.Start = InputApi.GetKeyCode("L");
			} else if(type == ControllerType.WsController) {
				m.B = InputApi.GetKeyCode("Z");
				m.A = InputApi.GetKeyCode("X");
				m.GenericKey1 = InputApi.GetKeyCode("Q");
				m.Start = InputApi.GetKeyCode("E");
				m.U = InputApi.GetKeyCode("Up Arrow");
				m.D = InputApi.GetKeyCode("Down Arrow");
				m.L = InputApi.GetKeyCode("Left Arrow");
				m.R = InputApi.GetKeyCode("Right Arrow");
			} else if(type == ControllerType.WsControllerVertical) {
				m.B = InputApi.GetKeyCode("X");
				m.A = InputApi.GetKeyCode("Z");
				m.GenericKey1 = InputApi.GetKeyCode("Q");
				m.Start = InputApi.GetKeyCode("E");
				
				m.U = InputApi.GetKeyCode("A");
				m.D = InputApi.GetKeyCode("D");
				m.L = InputApi.GetKeyCode("S");
				m.R = InputApi.GetKeyCode("W");

				m.Up = InputApi.GetKeyCode("Left Arrow");
				m.Down = InputApi.GetKeyCode("Right Arrow");
				m.Left = InputApi.GetKeyCode("Down Arrow");
				m.Right = InputApi.GetKeyCode("Up Arrow");
			} else {
				m.TurboA = InputApi.GetKeyCode(";");
				m.TurboB = InputApi.GetKeyCode("M");
				m.Select = InputApi.GetKeyCode("U");
				m.Start = InputApi.GetKeyCode("I");
			}
		}

		public static void ApplyArrowLayout(KeyMapping m, ControllerType type)
		{
			m.ClearKeys(type);

			// Standard emulator layout: Arrow keys for D-Pad, X=A, Z=B, Enter=Start, RShift=Select
			m.A = InputApi.GetKeyCode("X");
			m.B = InputApi.GetKeyCode("Z");
			m.Up = InputApi.GetKeyCode("Up Arrow");
			m.Down = InputApi.GetKeyCode("Down Arrow");
			m.Left = InputApi.GetKeyCode("Left Arrow");
			m.Right = InputApi.GetKeyCode("Right Arrow");
			m.Start = InputApi.GetKeyCode("Enter");
			m.Select = InputApi.GetKeyCode("Right Shift");

			if(type == ControllerType.SnesController || type == ControllerType.SnesRumbleController) {
				m.X = InputApi.GetKeyCode("S");
				m.Y = InputApi.GetKeyCode("A");
				m.L = InputApi.GetKeyCode("Q");
				m.R = InputApi.GetKeyCode("W");
			} else if(type == ControllerType.PceAvenuePad6) {
				m.Y = InputApi.GetKeyCode("A");
				m.L = InputApi.GetKeyCode("S");
				m.R = InputApi.GetKeyCode("D");
				m.X = InputApi.GetKeyCode("C");
			} else if(type == ControllerType.GbaController) {
				m.L = InputApi.GetKeyCode("A");
				m.R = InputApi.GetKeyCode("S");
			} else if(type == ControllerType.WsController) {
				m.GenericKey1 = InputApi.GetKeyCode("Tab");
				m.U = InputApi.GetKeyCode("W");
				m.D = InputApi.GetKeyCode("S");
				m.L = InputApi.GetKeyCode("A");
				m.R = InputApi.GetKeyCode("D");
			} else if(type == ControllerType.WsControllerVertical) {
				m.GenericKey1 = InputApi.GetKeyCode("Tab");
				m.Up = InputApi.GetKeyCode("A");
				m.Down = InputApi.GetKeyCode("D");
				m.Left = InputApi.GetKeyCode("S");
				m.Right = InputApi.GetKeyCode("W");

				m.U = InputApi.GetKeyCode("Left Arrow");
				m.D = InputApi.GetKeyCode("Right Arrow");
				m.L = InputApi.GetKeyCode("Down Arrow");
				m.R = InputApi.GetKeyCode("Up Arrow");
			} else {
				// NES/GB - add turbo buttons
				m.TurboA = InputApi.GetKeyCode("C");
				m.TurboB = InputApi.GetKeyCode("V");
			}
		}

		public static void ApplyXboxLayout(KeyMapping m, int player, ControllerType type, bool altLayout = false)
		{
			m.ClearKeys(type);

			string prefix = "Pad" + (player + 1).ToString() + " ";
			// A (jump) = A on Xbox (X on PS), B (shoot) = X on Xbox (Square on PS)
			m.A = InputApi.GetKeyCode(prefix + "A");
			m.B = InputApi.GetKeyCode(prefix + "X");
			m.Select = InputApi.GetKeyCode(prefix + "Select");
			m.Start = InputApi.GetKeyCode(prefix + "Start");
			m.Up = InputApi.GetKeyCode(prefix + "Up");
			m.Down = InputApi.GetKeyCode(prefix + "Down");
			m.Left = InputApi.GetKeyCode(prefix + "Left");
			m.Right = InputApi.GetKeyCode(prefix + "Right");

			if(type == ControllerType.SnesController || type == ControllerType.PceAvenuePad6 || type == ControllerType.GbaController || type == ControllerType.SnesRumbleController) {
				m.X = InputApi.GetKeyCode(prefix + "Y");
				m.Y = InputApi.GetKeyCode(prefix + "B");
				m.L = InputApi.GetKeyCode(prefix + "L1");
				m.R = InputApi.GetKeyCode(prefix + "R1");
			} else if(type == ControllerType.WsController) {
				m.GenericKey1 = InputApi.GetKeyCode(prefix + "Back");
				m.U = InputApi.GetKeyCode(prefix + "RT Up");
				m.D = InputApi.GetKeyCode(prefix + "RT Down");
				m.L = InputApi.GetKeyCode(prefix + "RT Left");
				m.R = InputApi.GetKeyCode(prefix + "RT Right");
			} else if(type == ControllerType.WsControllerVertical) {
				m.GenericKey1 = InputApi.GetKeyCode(prefix + "Back");

				m.A = InputApi.GetKeyCode(prefix + "L1");
				m.B = InputApi.GetKeyCode(prefix + "R1");

				m.U = InputApi.GetKeyCode(prefix + "Left");
				m.D = InputApi.GetKeyCode(prefix + "Right");
				m.L = InputApi.GetKeyCode(prefix + "Down");
				m.R = InputApi.GetKeyCode(prefix + "Up");

				m.Up = InputApi.GetKeyCode(prefix + "RT Left");
				m.Down = InputApi.GetKeyCode(prefix + "RT Right");
				m.Left = InputApi.GetKeyCode(prefix + "RT Up");
				m.Right = InputApi.GetKeyCode(prefix + "RT Down");
			} else {
				// TurboA = Y on Xbox (Triangle on PS), TurboB = B on Xbox (Circle on PS)
				m.TurboA = InputApi.GetKeyCode(prefix + "Y");
				m.TurboB = InputApi.GetKeyCode(prefix + "B");
			}
		}

		public static void ApplyPs4Layout(KeyMapping m, int player, ControllerType type, bool altLayout = false)
		{
			m.ClearKeys(type);

			string prefix = "Joy" + (player + 1).ToString() + " ";
			// A (jump) = X/Cross (But2), B (shoot) = Square (But1)
			m.A = InputApi.GetKeyCode(prefix + "But2");
			m.B = InputApi.GetKeyCode(prefix + "But1");
			m.Select = InputApi.GetKeyCode(prefix + "But9");
			m.Start = InputApi.GetKeyCode(prefix + "But10");
			m.Up = InputApi.GetKeyCode(prefix + "DPad Up");
			m.Down = InputApi.GetKeyCode(prefix + "DPad Down");
			m.Left = InputApi.GetKeyCode(prefix + "DPad Left");
			m.Right = InputApi.GetKeyCode(prefix + "DPad Right");
			if(type == ControllerType.SnesController || type == ControllerType.PceAvenuePad6 || type == ControllerType.GbaController || type == ControllerType.SnesRumbleController) {
				m.X = InputApi.GetKeyCode(prefix + "But4");
				m.Y = InputApi.GetKeyCode(prefix + "But3");
				m.L = InputApi.GetKeyCode(prefix + "But5");
				m.R = InputApi.GetKeyCode(prefix + "But6");
			} else if(type == ControllerType.WsController) {
				m.GenericKey1 = InputApi.GetKeyCode(prefix + "But9");
				m.U = InputApi.GetKeyCode(prefix + "Y2+");
				m.D = InputApi.GetKeyCode(prefix + "Y2-");
				m.L = InputApi.GetKeyCode(prefix + "X2-");
				m.R = InputApi.GetKeyCode(prefix + "X2+");
			} else if(type == ControllerType.WsControllerVertical) {
				m.GenericKey1 = InputApi.GetKeyCode(prefix + "But9");

				m.A = InputApi.GetKeyCode(prefix + "But5");
				m.B = InputApi.GetKeyCode(prefix + "But6");

				m.U = InputApi.GetKeyCode(prefix + "DPad Left");
				m.D = InputApi.GetKeyCode(prefix + "DPad Right");
				m.L = InputApi.GetKeyCode(prefix + "DPad Down");
				m.R = InputApi.GetKeyCode(prefix + "DPad Up");

				m.Up = InputApi.GetKeyCode(prefix + "X2-");
				m.Down = InputApi.GetKeyCode(prefix + "X2+");
				m.Left = InputApi.GetKeyCode(prefix + "Y2+");
				m.Right = InputApi.GetKeyCode(prefix + "Y2-");
			} else {
				// TurboA = Triangle (But4), TurboB = Circle (But3)
				m.TurboA = InputApi.GetKeyCode(prefix + "But4");
				m.TurboB = InputApi.GetKeyCode(prefix + "But3");
			}
		}
	}
}
