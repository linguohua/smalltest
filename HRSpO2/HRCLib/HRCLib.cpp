// This is the main DLL file.

#include "stdafx.h"

#include "HRCLib.h"

#include "max30102_algo.h"

static int32_t spo2;
static int8_t spo2Valid;
static int32_t hr;
static int8_t hrValid;

void HRCLib::HRCLib::GetDxBeforeHamming(array<int>^ buffer)
{
	pin_ptr<int> pbuffer = &buffer[0];
	int length = buffer->Length;

	maxim_get_an_dx_before_hamming(pbuffer, length);
}

void HRCLib::HRCLib::GetDx(array<int>^ buffer)
{
	pin_ptr<int> pbuffer = &buffer[0];
	int length = buffer->Length;

	maxim_get_an_dx(pbuffer, length);
}

void HRCLib::HRCLib::HRCalc(array<int>^ irBuffer, array<int>^ rBuffer)
{
	pin_ptr<int> pIRbuffer = &irBuffer[0];
	pin_ptr<int> pRbuffer = &rBuffer[0];

	int length = irBuffer->Length;

	maxim_heart_rate_and_oxygen_saturation((uint32_t*)((int32_t*)pIRbuffer), length,
		(uint32_t*)((int32_t*)pRbuffer), &spo2, &spo2Valid, &hr, &hrValid);
}

int HRCLib::HRCLib::BufferSizeSupported()
{
	return BUFFER_SIZE;
}

int HRCLib::HRCLib::GetSpo2()
{
	return spo2;
}

int HRCLib::HRCLib::GetHeartRate()
{
	return hr;
}
