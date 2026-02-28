using System.Collections.Generic;
using Microsoft.Win32;

namespace PrimevalTitmouse
{
  public class Debuff
  {

    public string debuff_id;

    public Debuff(string id)
    {
      Debuff debuff;

      if (string.IsNullOrEmpty(id) || !Regression.generalData.Debuffs.TryGetValue(id, out debuff))
      {
        this.debuff_id = "Regression.Wet";
        this._name = "Wet";
        this._type = "Defence";
        this._descriptions = new string[] { "Your pants are wet!" }
        ;
      }
      else
      {
        this.debuff_id = id;
        this._name = debuff.name;
        this._type = debuff.type;
        this._descriptions = debuff.descriptions;
      }
    }

    private string _name = "";
    public string name
    {
      get => _name;
      set => _name = value;
    }

    private string _type = "";
    public string type
    {
      get => _type;
      set => _type = value;
    }

    private string[] _descriptions = {""};
    public string[] descriptions
    {
      get => _descriptions;
      set => _descriptions = value;
    }
  }
}
