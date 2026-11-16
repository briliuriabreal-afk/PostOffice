using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PostOffice.Infrastructure;

namespace PostOffice.Views;

// Окно ввода значений; состав полей задаётся описаниями. Окно только собирает
// введённое, что делать со значениями, решает модель представления
public partial class InputWindow : Window
{

    // введённые значения по одному на поле, в том же порядке:
    // выбранный в списке объект или строка из поля ввода
    public List<object> Values { get; private set; } = [];

    // описания полей и поля окна: подпись, список значений, поле свободного ввода
    private readonly InputField[] _fields;
    private readonly List<TextBlock> _prompts;
    private readonly List<ComboBox> _lists;
    private readonly List<TextBox> _boxes;


    public InputWindow(string title, params InputField[] fields) {
        InitializeComponent();

        Title = title;

        _fields = fields;
        _prompts = [TblPrompt1, TblPrompt2, TblPrompt3, TblPrompt4, TblPrompt5];
        _lists = [CbxValue1, CbxValue2, CbxValue3, CbxValue4, CbxValue5];
        _boxes = [TbxValue1, TbxValue2, TbxValue3, TbxValue4, TbxValue5];

        for (var i = 0; i < _prompts.Count; i++) {
            if (i >= fields.Length) {
                _prompts[i].Visibility = Visibility.Collapsed;
                _lists[i].Visibility = Visibility.Collapsed;
                _boxes[i].Visibility = Visibility.Collapsed;
                continue;
            } // if

            _prompts[i].Text = fields[i].Prompt;

            // закрытый набор значений выбирается списком, произвольное значение вводится
            if (fields[i].Values.Cast<object>().Any()) {
                _lists[i].DisplayMemberPath = fields[i].DisplayPath;
                _lists[i].ItemsSource = fields[i].Values;
                _lists[i].SelectedIndex = 0;
                _boxes[i].Visibility = Visibility.Collapsed;
                continue;
            } // if

            _lists[i].Visibility = Visibility.Collapsed;
        } // for i

        // ввод начинается с первого поля
        FocusManager.SetFocusedElement(this, _lists[0].Visibility == Visibility.Visible ? _lists[0] : _boxes[0]);
    } // InputWindow


    private void BtnOk_Click(object sender, RoutedEventArgs e) {
        List<object> values = [];

        for (var i = 0; i < _prompts.Count; i++) {
            if (_prompts[i].Visibility != Visibility.Visible) {
                continue;
            } // if

            if (_lists[i].Visibility == Visibility.Visible) {
                values.Add(_lists[i].SelectedItem);
                continue;
            } // if

            // сообщаем об ошибке, значения не правим
            if (_boxes[i].Text.Length == 0 && !_fields[i].Optional) {
                Utils.Fail($"Заполните поле «{_prompts[i].Text}»");
                return;
            } // if

            values.Add(_boxes[i].Text);
        } // for i

        Values = values;
        DialogResult = true;
    } // BtnOk_Click

} // class InputWindow
