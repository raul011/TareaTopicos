#r "nuget: BCrypt.Net-Next, 4.0.3"
using System;

string ci = "7894561";
string hash = BCrypt.Net.BCrypt.HashPassword(ci);
Console.WriteLine(hash);
