using AOBot_Testing.Structures;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    public class INIParserTests
    {
        [OneTimeSetUp]
        public void GatherAllINI()
        {
            CharacterINI.RefreshCharacterList();
        }

        [Test]
        public void CustomTestINI() 
        {

        }
    }
}
