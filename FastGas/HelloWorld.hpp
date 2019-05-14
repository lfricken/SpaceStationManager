#pragma once

#include <iostream>
#include "FastGasLib.hpp"


extern "C" FASTGAS_API int add(int a, int b);


class FASTGAS_API HelloWorld
{
public:
	HelloWorld();
	~HelloWorld();

	void SayThis(int i);
};
