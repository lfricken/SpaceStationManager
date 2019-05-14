#pragma once

#ifdef FASTGAS_EXPORTS
#define FASTGAS_API __declspec(dllexport)
#else
#define FASTGAS_API __declspec(dllimport)
#endif
