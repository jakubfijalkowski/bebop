Pierogi.g4 describes the Pierogi packet specification format as an ANTLR v4 grammar.

See: [Getting Started with ANTLR v4](https://github.com/antlr/antlr4/blob/master/doc/getting-started.md#installation). The long story short is that you do this to play with it:

```bash
cd /usr/local/lib
curl -O https://www.antlr.org/download/antlr-4.7.1-complete.jar
export CLASSPATH=".:/usr/local/lib/antlr-4.7.1-complete.jar:$CLASSPATH"
alias antlr4='java -Xmx500M -cp "/usr/local/lib/antlr-4.7.1-complete.jar:$CLASSPATH" org.antlr.v4.Tool'
alias grun='java -Xmx500M -cp "/usr/local/lib/antlr-4.7.1-complete.jar:$CLASSPATH" org.antlr.v4.gui.TestRig'
antlr4 Pierogi.g4 && javac Pierogi*.java && grun Pierogi schema -tree  < ../../Schemas/sample.pie
```

[antlr4cs](https://github.com/tunnelvisionlabs/antlr4cs/) is then supposedly able to output C# code for dealing with a Pierogi parse tree. But setting it up with VS 2019 is apparently not easy. I'll have to explore the options here.
