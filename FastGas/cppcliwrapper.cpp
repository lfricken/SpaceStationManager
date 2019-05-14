#include <string>
#include <Windows.h>
#include "cppcliwrapper.hpp"
#include <msclr\marshal_cppstd.h>


using namespace cppcliwrapper;
using namespace std;

ManagedHelloWorld::ManagedHelloWorld() : impl(new HelloWorld())
{

}
ManagedHelloWorld::~ManagedHelloWorld()
{
	delete impl;
}
int ManagedHelloWorld::SayThis(int i)
{
	impl->SayThis(i);
	return 33;
}

