using System.Collections.Generic;

[System.Serializable]
public class FlightsEmbeddedField
{
    //public string airline_name;
    public string reg_number = "empty";
    public float lat;
    public float lng;
    public float alt;
    public float dir;
    public string arr_iata;
}

[System.Serializable]
public class flights
{
    public List<FlightsEmbeddedField> response = null; // Embedded field in JSON
}