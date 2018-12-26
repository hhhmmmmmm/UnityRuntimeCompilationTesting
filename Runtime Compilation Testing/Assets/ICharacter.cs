
// note: this interface will be referenced in loaded source code:
public interface ICharacter
{
    int Height { get; }
    string Language { get; }
}

/// <summary>
/// EXAMPLE of base game type. 
/// From custom.txt in StreamingAssets we compile in new kinds, Elf and Dwarf.
/// </summary>
public class Human : ICharacter
{
    public int Height { get { return UnityEngine.Random.Range(150, 200); } }
    public string Language { get { return "Common"; } }
}