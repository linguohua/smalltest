// HRCLib.h

#pragma once

using namespace System;

namespace HRCLib {

	public ref class HRCLib
	{
		// TODO: Add your methods for this class here.
	public:
		static void GetDxBeforeHamming(array<int>^ buffer);

		static void GetDx(array<int>^ buffer);

		static void HRCalc(array<int>^ irBuffer, array<int>^ rBuffer);

		static int BufferSizeSupported();

		static int GetHeartRate();

		static int GetSpo2();
	};
}
