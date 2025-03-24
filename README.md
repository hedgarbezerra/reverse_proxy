Caso o certificado atual não seja mais válido, o script para atualizar um novo com .NET é o seguinte:
`dotnet dev-certs https -ep ./Certificates/aspnetapp.pfx -p 123 --trust` 