// Aurora-AsusAuraWrapper.cpp : Defines the exported functions for the DLL application.
//	Author: kekkokk https://github.com/kekkokk
//

#include "stdafx.h"
#include "Aurora-AsusAuraWrapper.h"
#include <iostream>

namespace AsusSdkWrapper {

	// LOAD
	bool AuraSdk::LoadDll() {

		hLib = LoadLibraryA("AURA_SDK.dll");
		DWORD lastError = GetLastError();

		if (hLib == nullptr) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper" << lastError << std::endl;
			return false;
		}

		(FARPROC&)EnumerateMbController = GetProcAddress(hLib, "EnumerateMbController");
		(FARPROC&)SetMbMode = GetProcAddress(hLib, "SetMbMode");
		(FARPROC&)SetMbColor = GetProcAddress(hLib, "SetMbColor");
		(FARPROC&)GetMbColor = GetProcAddress(hLib, "GetMbColor");
		(FARPROC&)GetMbLedCount = GetProcAddress(hLib, "GetMbLedCount");

		(FARPROC&)EnumerateGPU = GetProcAddress(hLib, "EnumerateGPU");
		(FARPROC&)SetGPUMode = GetProcAddress(hLib, "SetGPUMode");
		(FARPROC&)SetGPUColor = GetProcAddress(hLib, "SetGPUColor");
		(FARPROC&)GetGPULedCount = GetProcAddress(hLib, "GetGPULedCount");

		(FARPROC&)CreateClaymoreKeyboard = GetProcAddress(hLib, "CreateClaymoreKeyboard");
		(FARPROC&)SetClaymoreKeyboardMode = GetProcAddress(hLib, "SetClaymoreKeyboardMode");
		(FARPROC&)SetClaymoreKeyboardColor = GetProcAddress(hLib, "SetClaymoreKeyboardColor");
		(FARPROC&)GetClaymoreKeyboardLedCount = GetProcAddress(hLib, "GetClaymoreKeyboardLedCount");
		(FARPROC&)EnumerateMbController = GetProcAddress(hLib, "EnumerateMbController");

		(FARPROC&)CreateRogMouse = GetProcAddress(hLib, "CreateRogMouse");
		(FARPROC&)SetRogMouseMode = GetProcAddress(hLib, "SetRogMouseMode");
		(FARPROC&)SetRogMouseColor = GetProcAddress(hLib, "SetRogMouseColor");
		(FARPROC&)RogMouseLedCount = GetProcAddress(hLib, "RogMouseLedCount");

		// TRY TO ENUMERATE KEYBOARD
		try {
			_keyboardLightCtrl = new ClaymoreKeyboardLightControl;
			DWORD Create = CreateClaymoreKeyboard(_keyboardLightCtrl);
			if (Create > 0) {
				_isKeyboardPresent = true;
			}
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper: can't enumerate Keyboards" << std::endl;
		}

		// TRY TO ENUMERATE MB CONTROLLERS
		DWORD count = 0;
		try {
			count = EnumerateMbController(NULL, 0);
			_mbLightCtrl = new MbLightControl[count];
			EnumerateMbController(_mbLightCtrl, count);
		} catch (std::exception e) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper: can't enumerate Motherboards" << std::endl;
		}
		_mbLedControllers = count;

		// TRY TO ENUMERATE GPUS CONTROLLERS
		count = 0;
		try {
			count = EnumerateGPU(NULL, 0);
			_gpuLightCtrl = new GPULightControl[count];
			EnumerateGPU(_gpuLightCtrl, count);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper: can't enumerate GPUs" << std::endl;
		}
		_gpuLedControllers = count;

		// TRY TO ENUMERATE MOUSE
		try {
			_mouseLightCtrl = new RogMouseLightControl;
			DWORD Create = CreateRogMouse(_mouseLightCtrl);

			if (Create > 0) {
				_isMousePresent = true;
			}
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper: can't enumerate Mouses" << std::endl;

		}

		return true;
	}


	// MOTHERBOARDs
	void AuraSdk::SetMBLedMode(int controllerId, int mode) {
		if (controllerId < 0 || controllerId >= _mbLedControllers)
			return;
		try {
			SetMbMode(_mbLightCtrl[controllerId], mode);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot set mode " << mode << " for MB controller: " << controllerId << std::endl;
		}
	}

	void AuraSdk::SetMBLedColor(int controllerId, array<System::Byte>^ colors) {
		if (controllerId < 0 || controllerId >= _mbLedControllers)
			return;
		pin_ptr<Byte> p = &colors[0];
		try {
			SetMbColor(_mbLightCtrl[controllerId], p, colors->Length);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot update colors for MB controller: " << controllerId << std::endl;
		}
	}

	int AuraSdk::GetMBLedCount(int controllerId) {
		if (controllerId < 0 || controllerId >= _mbLedControllers)
			return -1;
		int count = 0;
		try {
			count = GetMbLedCount(_mbLightCtrl[controllerId]);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot get led count for MB controller: " << controllerId << std::endl;
		}
		return count;
	}


	// GPUs
	void AuraSdk::SetGPUCtrlLedMode(int controllerId, int mode) {
		if (controllerId < 0 || controllerId >= _gpuLedControllers)
			return;
		try {
			SetGPUMode(_gpuLightCtrl[controllerId], mode);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot set mode " << mode << " for GPU controller: " << controllerId << std::endl;
		}
	}

	void AuraSdk::SetGPUCtrlLedColor(int controllerId, array<System::Byte>^ colors) {
		if (controllerId < 0 || controllerId >= _gpuLedControllers)
			return;
		try {
			pin_ptr<Byte> p = &colors[0];
			SetGPUColor(_gpuLightCtrl[controllerId], p, colors->Length);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot update colors for GPU controller: " << controllerId << std::endl;
		}
	}

	int AuraSdk::GetGPUCtrlLedCount(int controllerId) {
		if (controllerId < 0 || controllerId >= _gpuLedControllers)
			return -1;
		int count = 0;
		try {
			count = GetGPULedCount(_gpuLightCtrl[controllerId]);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot get led count for GPU controller: " << controllerId << std::endl;
		}
		return count;
	}

	// KEYBOARD
	void AuraSdk::SetKeyboardLedMode(int mode) {
		if (!_isKeyboardPresent)
			return;
		try {
			SetClaymoreKeyboardMode(*_keyboardLightCtrl, mode);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot set mode: " << mode << " for Keyboard controller." << std::endl;
		}
	}

	void AuraSdk::SetKeyboardLedColor(array<System::Byte>^ colors) {
		if (!_isKeyboardPresent)
			return;
		try {
			pin_ptr<Byte> p = &colors[0];
			SetClaymoreKeyboardColor(*_keyboardLightCtrl, p, colors->Length);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot update colors for Keyboard controller." << std::endl;
		}

	}

	int AuraSdk::GetKeyboardLedCount() {
		if (!_isKeyboardPresent)
			return -1;
		int count = 0;
		try {
			count = GetClaymoreKeyboardLedCount(*_keyboardLightCtrl);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot get Keyboard led count." << std::endl;
		}
		return count;
	}


	// MOUSE
	void AuraSdk::SetMouseLedMode(int mode) {
		if (!_isMousePresent)
			return;
		try {
			SetRogMouseMode(*_mouseLightCtrl, mode);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot set mode: " << mode << " for mouse controller." << std::endl;
		}
	}

	void AuraSdk::SetMouseLedColor(array<System::Byte>^ colors) {
		if (!_isMousePresent)
			return;
		try {
			pin_ptr<Byte> p = &colors[0];
			SetRogMouseColor(*_mouseLightCtrl, p, colors->Length);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot get set mouse colors." << std::endl;
		}
	}

	int AuraSdk::GetMouseLedCount() {
		if (!_isMousePresent)
			return -1;
		int count = 0;
		try {
			count = RogMouseLedCount(*_mouseLightCtrl);
		} catch (const std::exception&) {
			std::cout << "[ ERROR ] Aurora-AsusAuraWrapper cannot get mouse led count." << std::endl;
		}
		return count;
	}


	// UNLOAD
	void AuraSdk::UnloadDll() {
		if (hLib != nullptr) {
			FreeLibrary(hLib);
			hLib = nullptr;
		}
	}

}