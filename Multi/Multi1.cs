using TestNS;

namespace Multi
{
    public class Multi1
    {
#if NETCOREAPP3_1
        TestCls.CoreClass a2;
#endif

#if NET5_0
        TestCls.CoreFiveClass a;
#endif
    }
}

#if NETCOREAPP3_1
class Warn { }

#endif


#if NET5_0
class Warn2 { }
#endif