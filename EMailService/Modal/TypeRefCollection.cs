using System;
using System.Collections.Generic;

namespace BottomhalfCore.BottomhalfModel
{
    public class TypeRefCollection
    {
        public TypeRefCollection()
        {
            this.Constructors = new Dictionary<int, ParameterDetail>();
            this.ClassRefNames = new List<ClassRefDetination>();
            this.IsFullyCreated = false;
            this.IsAOPEnabled = false;
        }
        public string AssemblyQualifiedName { set; get; }
        public string ClassName { set; get; }
        public Boolean IsAOPEnabled { set; get; }
        public string ClassFullyQualifiedName { set; get; }
        //public Type ClassType { set; get; }
        public Boolean IsFullyCreated { set; get; }
        public string AssemblyName { set; get; }
        public List<ClassRefDetination> ClassRefNames { set; get; }
        public IDictionary<int, ParameterDetail> Constructors { set; get; }
        public List<FieldAttributes> FieldAttributesCollection { set; get; }
        public List<ImplementedHirarchy> BaseTypeHirarchy { set; get; }
        public List<string> GenericTypeParamName { set; get; }
        public AopDetail AppliedAopDetail { set; get; }
        public string AliasName { set; get; }
        public List<AnnotationDefination> AnnotationNames { set; get; }
        public bool IsContainsGenericParameters { set; get; }
        public bool IsInterface { set; get; }
        public bool IsClass { set; get; }
        public bool IsScoped { set; get; }
        public bool IsTransient { set; get; }
        public bool IsSingleTon { set; get; }
    }

    public class AopDetail
    {
        public string AOPType { set; get; }
        public string ForWhichReturnType { set; get; }
        public string ForArgumentType { set; get; }
        public string ForNameSpace { set; get; }
        public string MethodExpression { set; get; }
        public string AspectClassName { set; get; }
        public string AspectFullyQualifiedName { set; get; }
    }

    public class ClassRefDetination
    {
        public ClassRefDetination()
        {
            ConstructorParameterDetail = new List<string>();
            AnnotationDetail = new List<AnnotationDefination>();
        }
        public List<string> ConstructorParameterDetail { set; get; }
        public string WrapperFullDescription { set; get; }
        public List<AnnotationDefination> AnnotationDetail { set; get; }

        // Remove below
        public List<string> TypesName { set; get; }
    }

    public class ParameterDetail
    {
        public ParameterDetail()
        {
            Parameters = new List<ParameterNameCollection>();
        }

        /*------------------------------------------  Dependent types -------------------------------------------*/
        public List<ParameterNameCollection> Parameters { set; get; }
        // If IsGeneric is true then it's indicate that the current parameter is generic type and type names  will be store in 
        // GenericParameterTypeName of type List<string>
        public Boolean IsGeneric { set; get; }
        public List<string> GenericParameterTypeName { set; get; }

        /*-------------------------------------------- End here ---------------------------------------------------*/
        public string WrapperFullDescription { set; get; }
        public List<AnnotationDefination> AnnotationDetail { set; get; }

        // Remove below
        public List<string> TypesName { set; get; }
    }

    public class ParameterNameCollection
    {
        public string Name { set; get; }
        public List<string> TypeName { set; get; }
        public string ArgumentName { set; get; }
        public List<ParameterNameCollection> ParameterStructurInfo { set; get; }
        public List<ParameterDetail> InnerParameterDetail { set; get; }
    }

    public class ImplementedHirarchy
    {
        public string TypeName { set; get; }
        public string TypeFullName { set; get; }
        public string ImplementedType { set; get; }
        public int IndexOrder { set; get; }
    }

    public class AnnotationDefination
    {
        public AnnotationDefination()
        {
            this.Value = new List<dynamic>();
        }
        public string AnnotationName { set; get; }
        public List<dynamic> Value { set; get; }
        public string AppliedOn { set; get; }
        public string Filename { set; get; }
        public int OrderNo { set; get; }
    }

    public class ClassAnnotationDetail
    {
        public List<AnnotationDefination> annotationDefination { set; get; }
        public List<AopDetail> aopDetailLst { set; get; }
        public bool IsCodeDocRequired { set; get; }
    }

    public class FieldAttributes
    {
        public FieldAttributes()
        {
            Attributes = new List<string>();
        }
        public string FieldName { set; get; }
        public string Name { set; get; }
        public string DeclaredName { set; get; }
        public Boolean IsGenericType { set; get; }
        public List<string> Attributes { set; get; }
        public List<string> GenericList { set; get; }
        public Boolean IsInterface { set; get; }
        public Boolean IsField { set; get; }
    }

    public enum ClassRefEnum
    {
        List = 1,
        Dictionary = 2,
        Hash = 3,
        Array = 4,
        ConcurrentDictionary = 5
    }
}
