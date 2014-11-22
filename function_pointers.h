#pragma once

#include <type_traits>
// Examples of using templates to generate function pointers.  Did not go into volatile but that
// is easy and requires const volatile and volatile.  These are very rare though
namespace daw {
	namespace impl {
		template<typename ResultType, typename... ArgTypes>
		struct make_function_pointer_impl {
			using type = typename std::add_pointer<ResultType(ArgTypes...)>::type;
		};
	}	// namespace impl

	// Create a pointer type to a function pointer of the form
	// Resulttype functionName( ArgsTypes )
	// e.g.
	// int blah( int, int, double ) { return 1; }
	// function_pointer_t<int, int, int, double> fp = &blah;
	template<typename ResultType, typename... ArgTypes>
	using function_pointer_t = typename impl::make_function_pointer_impl<ResultType, ArgTypes...>::type;

	namespace impl {
		template<typename ResultType, typename ClassType, typename... ArgTypes>
		struct make_member_function_pointer_impl {
			using type = ResultType(ClassType::*)(ArgTypes...);
		};

		template<typename ResultType, typename ClassType, typename... ArgTypes>
		struct make_const_member_function_pointer_impl {
			using type = ResultType(ClassType::*)(ArgTypes...) const;
		};
	}	// namespace impl

	// Create a pointer type to a member function pointer of the form
	// Resulttype functionName( ArgsTypes )
	// e.g.
	// struct A {
	// 	int blah( int, int, double ) const { return 1; }
	// };
	// member_function_pointer_t<int, A, int, int, double> fp = &A::blab;
	template<typename ResultType, typename ClassType, typename... ArgTypes>
	using member_function_pointer_t = typename impl::make_member_function_pointer_impl<ResultType, ClassType, ArgTypes...>::type;

	// Create a pointer type to a const member function pointer of the form
	// Resulttype functionName( ArgsTypes ) const
	// e.g.
	// struct A {
	// 	int blah( int, int, double ) const { return 1; }
	// };
	// member_function_pointer_t<int, A, int, int, double> fp = &A::blab;
	template<typename ResultType, typename ClassType, typename... ArgTypes>
	using const_member_function_pointer_t = typename impl::make_const_member_function_pointer_impl<ResultType, ClassType, ArgTypes...>::type;


}	// namespace daw
