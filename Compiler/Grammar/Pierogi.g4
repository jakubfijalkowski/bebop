grammar Pierogi;

COMMENT: '/*' .*? '*/' -> skip;
LINE_COMMENT: '//' ~[\r\n]* -> skip;
WHITESPACE: [ \t\r\n] -> skip;

UPPER_NAME: [A-Z_] [a-zA-Z0-9_]*;
LOWER_NAME: [a-z_] [a-zA-Z0-9_]*;
STRING: '"' ('\\\\' | '\\"' | ~[\\"])* '"';
DIGITS: [0-9]+;

packagePath: LOWER_NAME ('.' LOWER_NAME)*;
packageSpecifier: 'package' packagePath ';';

scalarType:
	'bool'
	| 'byte' // uint8
	| 'uint' // uint32
	| 'int' // int32
	| 'float' // double
	| 'string'
	| 'guid';

arrayType: scalarType '[]'; // no nested arrays
definedType: UPPER_NAME;

type: scalarType | arrayType | definedType;

deprecatedAnnotation: '[' 'deprecated' ('(' reason=STRING ')')? ']';
annotation: deprecatedAnnotation;

enumBranch: UPPER_NAME ('=' value=DIGITS)? ';';
enumDefinition: 'enum' UPPER_NAME '{' enumBranch+ '}';

structField: type LOWER_NAME annotation* ';';
structDefinition: 'readonly'? 'struct' UPPER_NAME '{' structField+ '}';

messageField: type LOWER_NAME ('=' index=DIGITS)? annotation* ';';
messageDefinition: 'message' UPPER_NAME '{' messageField+ '}';

definition:
	enumDefinition
	| structDefinition
	| messageDefinition;

schema: packageSpecifier? definition*;

