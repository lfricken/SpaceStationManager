#include <string>
#include <Windows.h>
#include "cppcliwrapper.hpp"
#include <msclr\marshal_cppstd.h>

/*
using namespace cppcliwrapper;
using namespace std;

void ManagedHelloWorld::InitializeLibrary(System::String^ path)
{
	msclr::interop::marshal_context context;
	std::string standardString = context.marshal_as<std::string>(path);

	LoadLibrary(standardString.c_str());
	// Actually load the delayed 
	// library from specific location
}

ManagedHelloWorld::ManagedHelloWorld() : hw(new HelloWorld())
{

}
ManagedHelloWorld::~ManagedHelloWorld()
{
	delete hw;
}
void ManagedHelloWorld::SayThis(int i)
{
	hw->SayThis(i);
}
*/
