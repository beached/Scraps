#pragma once

#include <type_traits>
// Examples of using templates to generate function pointers.  Including member function pointers 
// in the non-const/const and non-volatile/volatile combinations

namespace daw {
	namespace impl {
		template<typename ResultType, typename... ArgTypes>
		struct make_function_pointer_impl {
			using type = typename std::add_pointer<ResultType(ArgTypes...)>::type;
		};

		template<typename ResultType, typename ClassType, typename... ArgTypes>
		struct make_pointer_to_member_function_impl {
			using type = ResultType(ClassType::*)(ArgTypes...);
		};

		template<typename ResultType, typename ClassType, typename... ArgTypes>
		struct make_pointer_to_volatile_member_function_impl {
			using type = ResultType(ClassType::*)(ArgTypes...) volatile;
		};

		template<typename ResultType, typename ClassType, typename... ArgTypes>
		struct make_pointer_to_const_member_function_impl {
			using type = ResultType(ClassType::*)(ArgTypes...) const;
		};

		template<typename ResultType, typename ClassType, typename... ArgTypes>
		struct make_pointer_to_const_volatile_member_function_impl {
			using type = ResultType(ClassType::*)(ArgTypes...) const volatile;
		};
	}	// namespace impl

	// Create a pointer type to a function pointer of the form
	// Resulttype functionName( ArgsTypes )
	// e.g.
	// int blah( int, int, double ) { return 1; }
	// function_pointer_t<int, int, int, double> fp = &blah;
	template<typename ResultType, typename... ArgTypes>
	using function_pointer_t = typename impl::make_function_pointer_impl<ResultType, ArgTypes...>::type;

	// Create a pointer type to a member function pointer of the form
	// Resulttype functionName( ArgsTypes )
	// e.g.
	// struct A {
	// 	int blah( int, int, double ) { return 1; }
	// };
	// pointer_to_member_function_t<int, A, int, int, double> fp = &A::blab;
	template<typename ResultType, typename ClassType, typename... ArgTypes>
	using pointer_to_member_function_t = typename impl::make_pointer_to_member_function_impl<ResultType, ClassType, ArgTypes...>::type;

	// Create a pointer type to a const member function pointer of the form
	// Resulttype functionName( ArgsTypes )
	// e.g.
	// struct A {
	// 	int blah( int, int, double ) const { return 1; }
	// };
	// pointer_to_const_member_function_t<int, A, int, int, double> fp = &A::blab;
	template<typename ResultType, typename ClassType, typename... ArgTypes>
	using pointer_to_const_member_function_t = typename impl::make_pointer_to_const_member_function_impl<ResultType, ClassType, ArgTypes...>::type;

	// Create a pointer type to a volatile member function pointer of the form
	// Resulttype functionName( ArgsTypes )
	// e.g.
	// struct A {
	// 	int blah( int, int, double ) volatile { return 1; }
	// };
	// pointer_to_volatile_member_function_t<int, A, int, int, double> fp = &A::blab;
	template<typename ResultType, typename ClassType, typename... ArgTypes>
	using pointer_to_volatile_member_function_t = typename impl::make_pointer_to_volatile_member_function_impl<ResultType, ClassType, ArgTypes...>::type;


	// Create a pointer type to a const volatile member function pointer of the form
	// Resulttype functionName( ArgsTypes )
	// e.g.
	// struct A {
	// 	int blah( int, int, double ) const volatile { return 1; }
	// };
	// pointer_to_const_volatile_member_function_t<int, A, int, int, double> fp = &A::blab;
	template<typename ResultType, typename ClassType, typename... ArgTypes>
	using pointer_to_const_volatile_member_function_t = typename impl::make_pointer_to_const_volatile_member_function_impl<ResultType, ClassType, ArgTypes...>::type;


}	// namespace daw
