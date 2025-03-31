Caso o certificado atual não seja mais válido, o script para atualizar um novo com .NET é o seguinte:
`dotnet dev-certs https -ep ./Certificates/aspnetapp.pfx -p 123 --trust`


Para gerar token de acesso para o schema `Bearer` é necessário os seguinte passos:

1. Gerar uma nova chave de assinatura com o comando: `dotnet user-jwts key --reset`
1. Atualizar o token que fica no projeto, especificamente em `Constants > DotNetDefaults > Jwt > ApiKey`
1. Gerar o token de acesso com comando: `dotnet user-jwts create -p webapi01 -o token`