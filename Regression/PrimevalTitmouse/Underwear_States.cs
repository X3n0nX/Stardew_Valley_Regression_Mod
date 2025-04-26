using System.Collections.Generic;

namespace PrimevalTitmouse
{
  public class Underwear_States
  {
    public static readonly Single_State wet = new Single_State("wet");
    public static readonly Single_State messy= new Single_State("messy");
    public static readonly Single_State wet_messy = new Single_State("wet_and_messy");
    public static readonly Single_State drying = new Single_State("drying");


    public class Single_State
    {
      private readonly Dictionary<string, Dictionary<string, string>> UNDERWEAR_STATES = Regression.t.Underwear_States;

      private string _description;
      public string description
      {
        get => _description;
      }

      private string _display_name_description;
      public string display_name_description
      {
        get => _display_name_description;
      }

      public Single_State(string type)
      {
        const string KEY_DESCRIPTION = "text_description";
        const string KEY_DISPLAY_NAME_DESCRIPTION = "text_display_name";

        Dictionary<string, string> state;
        string desc;
        string desc_display_name;

        if (type != null && type != "" && UNDERWEAR_STATES.TryGetValue(type, out state) &&
          state.TryGetValue(KEY_DESCRIPTION, out desc) &&
          state.TryGetValue(KEY_DISPLAY_NAME_DESCRIPTION, out desc_display_name))
        {
          _description = desc;
          _display_name_description = desc_display_name;
        }
        else
        {
          _description = type;
          _display_name_description = type;
        }
      }
    }
  }
}
