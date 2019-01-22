#ifndef MAX30102_CONTROLLER_H
#define MAX30102_CONTROLLER_H

#include "app_util_platform.h"
#include "ble.h"
#include "stdbool.h"

#ifdef __cplusplus
extern "C" {
#endif

#define SAMPLE_RATE (100.0f)
#define RAW_DATA_BUFFER_SIZE 512

void feed_red_ired(float red, float ired);

#ifdef __cplusplus
}
#endif

#endif /*MAX30102_CONTROLLER_H*/
