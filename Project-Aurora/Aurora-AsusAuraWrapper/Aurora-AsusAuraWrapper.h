//	Author: kekkokk https://github.com/kekkokk

#pragma once

#include "AURALightingSDK.h"

using namespace System;

HMODULE hLib = nullptr;

EnumerateMbControllerFunc EnumerateMbController;
SetMbModeFunc SetMbMode;
SetMbColorFunc SetMbColor;
GetMbColorFunc GetMbColor;
GetMbLedCountFunc GetMbLedCount;

EnumerateGPUFunc EnumerateGPU;
SetGPUModeFunc SetGPUMode;
SetGPUColorFunc SetGPUColor;
GetGPULedCountFunc GetGPULedCount;

CreateClaymoreKeyboardFunc CreateClaymoreKeyboard;
SetClaymoreKeyboardModeFunc SetClaymoreKeyboardMode;
SetClaymoreKeyboardColorFunc SetClaymoreKeyboardColor;
GetClaymoreKeyboardLedCountFunc GetClaymoreKeyboardLedCount;

CreateRogMouseFunc CreateRogMouse;
SetRogMouseModeFunc SetRogMouseMode;
SetRogMouseColorFunc SetRogMouseColor;
RogMouseLedCountFunc RogMouseLedCount;



namespace AsusSdkWrapper {

	public ref class AuraSdk {
	public:
		// LOAD
		bool LoadDll();

		// MOTHERBOARDs
		int GetMBLedCount(int controllerId);
		void SetMBLedMode(int controllerId, int mode);
		void SetMBLedColor(int controllerId, array<System::Byte>^ colors);


		// GPUs
		int GetGPUCtrlLedCount(int controllerId);
		void SetGPUCtrlLedMode(int controllerId, int mode);
		void SetGPUCtrlLedColor(int controllerId, array<System::Byte>^ colors);


		// KEYBOARD
		int GetKeyboardLedCount();
		void SetKeyboardLedMode(int mode);
		void SetKeyboardLedColor(array<System::Byte>^ colors);


		// MOUSE
		int GetMouseLedCount();
		void SetMouseLedMode(int mode);
		void SetMouseLedColor(array<System::Byte>^ colors);


		// GETTERS
		bool isKeyboardPresent() { return _isKeyboardPresent; }
		bool isMousePresent() { return _isMousePresent; }
		int getMbAvailableControllers() { return _mbLedControllers; }
		int getGPUAvailableControllers() { return _gpuLedControllers; }

		// UNLOAD
		void UnloadDll();
	private:
		MbLightControl* _mbLightCtrl;
		GPULightControl* _gpuLightCtrl;
		ClaymoreKeyboardLightControl* _keyboardLightCtrl;
		RogMouseLightControl* _mouseLightCtrl;
		int _mbLedControllers;
		int _gpuLedControllers;
		bool _isKeyboardPresent;
		bool _isMousePresent;

	};

};