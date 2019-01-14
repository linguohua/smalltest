#ifndef MYUART_H
#define MYUART_H

#include "ble.h"
#include "stdbool.h"
#include "stddef.h"

#ifdef __cplusplus
extern "C" {
#endif

void myuart_init(bool async_mode);
void myuart_write(char const * p_buffer, size_t len);

void myuart_printf(char const *fmt, ...);

#ifdef __cplusplus
}
#endif

#endif /*MYUART_H*/
