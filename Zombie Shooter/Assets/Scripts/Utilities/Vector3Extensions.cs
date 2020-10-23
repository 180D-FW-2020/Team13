using UnityEngine;

public static class Vector3Extensions
{
    public static Vector2 xy(this Vector3 vector)
    {
        return new Vector2(vector.x, vector.y);
    }
    public static Vector2 xz(this Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }
    public static Vector2 yz(this Vector3 vector)
    {
        return new Vector2(vector.y, vector.z);
    }
    public static Vector2 yx(this Vector3 vector)
    {
        return new Vector2(vector.y, vector.x);
    }
    public static Vector2 zx(this Vector3 vector)
    {
        return new Vector2(vector.z, vector.x);
    }
    public static Vector2 zy(this Vector3 vector)
    {
        return new Vector2(vector.z, vector.y);
    }
}