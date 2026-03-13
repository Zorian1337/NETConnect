// Header file for Common PS4 headers that I need included

#pragma once

// Header paths must be included in the project for this to register
#ifdef _PS4
// Standard
#include "orbis/libkernel.h" // Kernel stuff

// Debugging
#include "../samples/_common/log.h" 
#include "errno.h" // For error handling (This is specifically for PS4) - could be custom or standard just ported to PS4, not sure yet


#endif