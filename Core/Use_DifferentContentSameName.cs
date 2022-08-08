using DifferentContentSameName;

namespace Core
{
    public partial class Use_DifferentContentSameName
    {
        [DifferentContentSameName]private int t = 1;
        [DifferentContentSameName]private int q = 2;
    }

    public class UsageOf_DifferentContentSameName{ 
        public void UseHere(){ 
            new Use_DifferentContentSameName();
            }
        }
}