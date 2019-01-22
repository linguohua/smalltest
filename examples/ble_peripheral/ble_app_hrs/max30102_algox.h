#ifndef MAX30102_ALGOX_H
#define MAX30102_ALGOX_H


#include "ble.h"
#include "stdbool.h"

#ifdef __cplusplus
extern "C" {
#endif

void move_average(int window, const float* ptrInput, int32_t length, float* ptrOutput);
void array_sub(const float* ptrSrc1, int32_t length, const float* ptrSrc2, float* ptrOutput);
void array_copy(const float* ptrSrc, int32_t length, float* ptrDst);

#ifdef __cplusplus
}
#endif

#endif /*MAX30102_ALGOX_H*/
