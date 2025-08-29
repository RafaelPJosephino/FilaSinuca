namespace FilaSinuca.Models;

public class Pessoa
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Nome { get; set; } = string.Empty;
}
