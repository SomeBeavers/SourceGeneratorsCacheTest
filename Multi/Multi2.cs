using DifferentContentSameName;


namespace Multi
{
#if NETCOREAPP3_1
    public partial class Milti2
    {
        [DifferentContentSameName] public string s = "";
    }

#endif

#if NET5_0
    public partial class Milti2
    {
        [DifferentContentSameName] public string s1 = "";
    }
#endif
}