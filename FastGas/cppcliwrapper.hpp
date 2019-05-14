#pragma once

#include "HelloWorld.hpp"

namespace cppcliwrapper
{
	public ref class ManagedHelloWorld
	{
	private:
		HelloWorld* impl;
	public:
		ManagedHelloWorld();
		~ManagedHelloWorld();
		int SayThis(int i);
	};
}
