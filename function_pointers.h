#pragma once

#include <type_traits>

namespace daw {
	namespace impl {
		template<typename ResultType, typename... ArgTypes>
		struct make_function_pointer_impl {
			using type = typename std::add_pointer<ResultType(ArgTypes...)>::type;
		};
	}	// namespace impl

	template<typename ResultType, typename... ArgTypes>
	using function_pointer_t = typename impl::make_function_pointer_impl<ResultType, ArgTypes...>::type;

	namespace impl {
		template<typename ResultType, typename ClassType, typename... ArgTypes>
		struct make_member_function_pointer_impl {
			typedef ResultType (ClassType::*type)(ArgTypes...);
		};
	}	// namespace impl

	template<typename ResultType, typename ClassType, typename... ArgTypes>
	using member_function_pointer_t = typename impl::make_member_function_pointer_impl<ResultType, ClassType, ArgTypes...>::type;


}	// namespace daw
