using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MyPad
{
    public static class DependencyPropertyExtensions
    {
        private const string DEPENDENCY_PROPERTY_SUFFIX = "Property";
        private const string DEPENDENCY_PROPERTY_KEY_SUFFIX = "PropertyKey";

        public static DependencyProperty Register(PropertyMetadata typeMetadata = null, ValidateValueCallback validateValueCallback = null, [CallerMemberName] string dependencyPropertyName = "")
        {
            if (dependencyPropertyName.EndsWith(DEPENDENCY_PROPERTY_SUFFIX) == false)
                throw new ArgumentException($"依存関係プロパティの変数名は {DEPENDENCY_PROPERTY_SUFFIX} で終わるように定義してください。", dependencyPropertyName);

            var name = dependencyPropertyName.Substring(0, dependencyPropertyName.Length - DEPENDENCY_PROPERTY_SUFFIX.Length);
            var ownerType = new StackFrame(1).GetMethod().ReflectedType;
            var propertyType = ownerType.GetProperty(name)?.PropertyType;
            if (propertyType == null)
                throw new InvalidOperationException($"パブリックプロパティ {name} を定義してください。この情報をもとに依存関係プロパティの名称と型を特定します。");

            return DependencyProperty.Register(
                name,
                propertyType,
                ownerType,
                typeMetadata,
                validateValueCallback);
        }

        public static DependencyProperty RegisterAttached(PropertyMetadata typeMetadata = null, ValidateValueCallback validateValueCallback = null, [CallerMemberName] string dependencyPropertyName = "")
        {
            if (dependencyPropertyName.EndsWith(DEPENDENCY_PROPERTY_SUFFIX) == false)
                throw new ArgumentException($"依存関係プロパティの変数名は {DEPENDENCY_PROPERTY_SUFFIX} で終わるように定義してください。", dependencyPropertyName);

            var name = dependencyPropertyName.Substring(0, dependencyPropertyName.Length - DEPENDENCY_PROPERTY_SUFFIX.Length);
            var ownerType = new StackFrame(1).GetMethod().ReflectedType;
            var propertyType = ownerType.GetMethod($"Get{name}")?.ReturnType;
            if (propertyType == null)
                throw new InvalidOperationException($"パブリックメソッド Get{name} を定義してください。この情報をもとに添付プロパティの名称と型を特定します。");

            return DependencyProperty.RegisterAttached(
                name,
                propertyType,
                ownerType,
                typeMetadata,
                validateValueCallback);
        }

        public static DependencyPropertyKey RegisterReadOnly(PropertyMetadata typeMetadata = null, ValidateValueCallback validateValueCallback = null, [CallerMemberName] string dependencyPropertyName = "")
        {
            if (dependencyPropertyName.EndsWith(DEPENDENCY_PROPERTY_KEY_SUFFIX) == false)
                throw new ArgumentException($"依存関係プロパティの識別子は {DEPENDENCY_PROPERTY_KEY_SUFFIX} で終わるように定義してください。", dependencyPropertyName);

            var name = dependencyPropertyName.Substring(0, dependencyPropertyName.Length - DEPENDENCY_PROPERTY_KEY_SUFFIX.Length);
            var ownerType = new StackFrame(1).GetMethod().ReflectedType;
            var propertyType = ownerType.GetProperty(name)?.PropertyType;
            if (propertyType == null)
                throw new InvalidOperationException($"パブリックプロパティ {name} を定義してください。この情報をもとに依存関係プロパティの名称と型を特定します。");

            return DependencyProperty.RegisterReadOnly(
                name,
                propertyType,
                ownerType,
                typeMetadata,
                validateValueCallback);
        }

        public static DependencyPropertyKey RegisterAttachedReadOnly(PropertyMetadata typeMetadata = null, ValidateValueCallback validateValueCallback = null, [CallerMemberName] string dependencyPropertyName = "")
        {
            if (dependencyPropertyName.EndsWith(DEPENDENCY_PROPERTY_KEY_SUFFIX) == false)
                throw new ArgumentException($"依存関係プロパティの識別子は {DEPENDENCY_PROPERTY_KEY_SUFFIX} で終わるように定義してください。", dependencyPropertyName);

            var name = dependencyPropertyName.Substring(0, dependencyPropertyName.Length - DEPENDENCY_PROPERTY_KEY_SUFFIX.Length);
            var ownerType = new StackFrame(1).GetMethod().ReflectedType;
            var propertyType = ownerType.GetProperty(name)?.PropertyType;
            if (propertyType == null)
                throw new InvalidOperationException($"パブリックプロパティ {name} を定義してください。この情報をもとに依存関係プロパティの名称と型を特定します。");

            return DependencyProperty.RegisterAttachedReadOnly(
                name,
                propertyType,
                ownerType,
                typeMetadata,
                validateValueCallback);
        }
    }
}