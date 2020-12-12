using System.Collections.Generic;
using System.Linq;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Attributes;
using Core.Meta.Interfaces;

namespace Core.Meta
{
    public readonly struct Definition : IDefinition
    {
        public Definition(string name,
            bool isReadOnly,
            in Span span,
            AggregateKind kind,
            ICollection<IField> fields,
            string documentation,
            IReadOnlyList<BaseAttribute> customAttributes)
        {
            Name = name;
            IsReadOnly = isReadOnly;
            Span = span;
            Kind = kind;
            Fields = fields;
            Documentation = documentation;
            CustomAttributes = customAttributes;
        }

        public string Name { get; }
        public Span Span { get; }
        public AggregateKind Kind { get; }
        public bool IsReadOnly { get; }
        public ICollection<IField> Fields { get; }
        public string Documentation { get; }
        public IReadOnlyList<BaseAttribute> CustomAttributes { get; }

        public BaseAttribute? OpcodeAttribute => CustomAttributes.FirstOrDefault(a => a is OpcodeAttribute);
        public BaseAttribute? CommandAttribute => CustomAttributes.FirstOrDefault(a => a is CommandAttribute);
        public BaseAttribute? QueryAttribute => CustomAttributes.FirstOrDefault(a => a is QueryAttribute);
        public BaseAttribute? AuthorizeWhenHasAnyOfAttribute => CustomAttributes.FirstOrDefault(a => a is AuthorizeWhenHasAnyOfAttribute);
    }
}
