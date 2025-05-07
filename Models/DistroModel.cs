using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSLWpfApp.Models
{
    public class DistroModel
    {
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string State { get; set; }
        // Constructor
        public DistroModel(string name, string version, string state, bool isDefault)
        {
            Name = name;
            Version = version;
            State = state;
            IsDefault = isDefault;
        }
        // Empty constructor for serialization
        public DistroModel()
        {
        }
    }
}
