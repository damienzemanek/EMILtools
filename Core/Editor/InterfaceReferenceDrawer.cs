using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InterfaceReference<>))]
[CustomPropertyDrawer(typeof(InterfaceReference<,>))]
public class InterfaceReferenceDrawer : MonoBehaviour
{
    private const string UNDERLYING_VALUE_FIELD_NAME = "underlyingValue";
    

}
