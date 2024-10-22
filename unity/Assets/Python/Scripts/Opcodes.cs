namespace Python
{
    public enum Opcode
    {
        /**************************/
        NO_OP, //0
        /**************************/
        POP_TOP, // 1
        DUP_TOP, //2
        ROT_TWO, //3
        ROT_THREE, //4
        PRINT_EXPR, //5
        /**************************/
        LOAD_CONST, //6
        LOAD_NONE, //7
        LOAD_TRUE, //8
        LOAD_FALSE, //9
        LOAD_INTEGER, //10
        LOAD_ELLIPSIS, //11
        LOAD_FUNCTION, //12
        LOAD_NULL, //13
        /**************************/
        LOAD_FAST, //14
        LOAD_NAME, // 15
        LOAD_NONLOCAL,//16
        LOAD_GLOBAL,//17
        LOAD_ATTR,//18
        LOAD_CLASS_GLOBAL, //19
        LOAD_METHOD, //20
        LOAD_SUBSCR, //21

        STORE_FAST, //22
        STORE_NAME, //23
        STORE_GLOBAL, //24
        STORE_ATTR, //25
        STORE_SUBSCR, //26

        DELETE_FAST,
        DELETE_NAME,
        DELETE_GLOBAL,
        DELETE_ATTR,
        DELETE_SUBSCR,
        /**************************/
        BUILD_LONG,
        BUILD_IMAG,
        BUILD_BYTES,//34
        BUILD_TUPLE,
        BUILD_LIST,//36
        BUILD_DICT,
        BUILD_SET,//38
        BUILD_SLICE,
        BUILD_STRING,//40
        BUILD_CSTRING,
        /**************************/
        BUILD_TUPLE_UNPACK,
        BUILD_LIST_UNPACK,
        BUILD_DICT_UNPACK,
        BUILD_SET_UNPACK,
        /**************************/
        BINARY_TRUEDIV,
        BINARY_POW,

        BINARY_ADD,
        BINARY_SUB,
        BINARY_MUL,
        BINARY_FLOORDIV,
        BINARY_MOD,//52

        COMPARE_LT,//53
        COMPARE_LE,//54
        COMPARE_EQ,//55
        COMPARE_NE,//56
        COMPARE_GT,//57
        COMPARE_GE,//58

        BITWISE_LSHIFT,
        BITWISE_RSHIFT,
        BITWISE_AND,
        BITWISE_OR,
        BITWISE_XOR,

        BINARY_MATMUL,

        IS_OP,//65
        CONTAINS_OP,
        /**************************/
        JUMP_ABSOLUTE,//67
        JUMP_ABSOLUTE_TOP,
        POP_JUMP_IF_FALSE,//69
        POP_JUMP_IF_TRUE,//70
        JUMP_IF_TRUE_OR_POP,
        JUMP_IF_FALSE_OR_POP,
        SHORTCUT_IF_FALSE_OR_POP,
        LOOP_CONTINUE,//74
        LOOP_BREAK,//75
        GOTO,
        /**************************/
        FSTRING_EVAL,//77
        REPR,
        CALL,//79
        CALL_TP,
        RETURN_VALUE,
        YIELD_VALUE,
        /**************************/
        LIST_APPEND,
        DICT_ADD,
        SET_ADD,
        /**************************/
        UNARY_NEGATIVE,
        UNARY_NOT,
        UNARY_STAR,
        UNARY_INVERT,
        /**************************/
        GET_ITER,
        FOR_ITER,
        /**************************/
        IMPORT_NAME,
        IMPORT_STAR,
        /**************************/
        UNPACK_SEQUENCE,
        UNPACK_EX,
        /**************************/
        BEGIN_CLASS,
        END_CLASS,
        STORE_CLASS_ATTR,
        BEGIN_CLASS_DECORATION,
        END_CLASS_DECORATION,
        ADD_CLASS_ANNOTATION,
        /**************************/
        WITH_ENTER,
        WITH_EXIT,
        /**************************/
        EXCEPTION_MATCH,
        RAISE, //105
        ASSERT,
        RE_RAISE,
        POP_EXCEPTION,
        /**************************/
        FORMAT_STRING,
        /**************************/
        INC_FAST,
        DEC_FAST,
        INC_GLOBAL,
        DEC_GLOBAL,
    }
}
