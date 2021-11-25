using MiniORM.Attributes;

namespace MiniORM.Models
{
    [Key("Person", "Id", "")]
    public class Person
    {
        [DataField("Id", "int")]
        private int Id { get; set; }

        [DataField("Name", "nvarchar(50)")]
        public string Name { get; set; }

        [DataField("Age", "int")]
        public int Age { get; set; }

        [DataField("Sex", "nvarchar(50)")]
        public string Sex { get; set; }
    }
}
