#include "stdafx.h"
#include "CppUnitTest.h"
#include "HelloWorld.hpp"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

namespace FastGasUnitTests
{
	TEST_CLASS(FastGasTest)
	{
	public:

		TEST_METHOD(Basic)
		{
			auto f = new HelloWorld();
			Assert::IsTrue(33 == f->SayThis(33));
		}
	};
}