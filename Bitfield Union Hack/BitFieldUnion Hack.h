template<unsigned bitno, unsigned nbits=1, typename T=u8>
struct RegBit {
	T data;
	enum { mask = (1u << nBits) - 1u };
	template<typename T2>
	RegBit& operator=( T2 val ) {
		data = (data & ~(mask << bitno)) | ((nbits > 1 ? val & mask : !!val) << bitno);
		return *this;
	}

	operator unsigned( ) const {
		return (data >> bitno) & mask;
	}
	RegBit& operator++ ( ) {
		return *this = *this + 1;
	}
	unsigned operator++( int ) {
		unsigned r = *this;
		++*this;
		return r;
	}
};


union {
	u8 val;
	RegBit(1) one;
	RegBit(2) two;
} flags;