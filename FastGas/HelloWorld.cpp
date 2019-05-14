#include "HelloWorld.hpp"

int add(int a, int b)
{
	return a + b;
}

HelloWorld::HelloWorld()
{

}
HelloWorld::~HelloWorld()
{

}
void HelloWorld::SayThis(int i)
{
	std::cout << "Hello World Says" << i;
}
