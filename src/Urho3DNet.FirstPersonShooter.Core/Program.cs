using System;

namespace Urho3DNet.FirstPersonShooter
{
    class Program
    {
        static void Main(string[] args)
        {
            Urho3DNet.Launcher.Run(_ => new FPSApplication(_));
        }
    }
}
