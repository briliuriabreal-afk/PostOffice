using System.Collections;

namespace PostOffice.Infrastructure;

// Описание одного поля окна ввода.
// Если набор допустимых значений не пуст, поле показывается выпадающим списком
// и возвращает выбранный объект; иначе - полем свободного ввода и возвращает строку
public class InputField(string prompt, IEnumerable values, string displayPath)
{

    // подпись поля
    public string Prompt { get; set; } = prompt;

    // допустимые значения; пустой набор - свободный ввод
    public IEnumerable Values { get; set; } = values;

    // свойство элемента, показываемое в списке
    public string DisplayPath { get; set; } = displayPath;

    // поле свободного ввода можно оставить пустым: тогда возвращается пустая строка
    public bool Optional { get; set; }


    // поле свободного ввода
    public InputField(string prompt) : this(prompt, new List<string>(), "") { } // InputField

} // class InputField
