[System.Serializable]
public class EmbeddedField
{
    public float lat;
    public float lng;
    public float alt;
    public float dir;
}

[System.Serializable]
public class airports
{
    public EmbeddedField response; // Embedded field in JSON
}