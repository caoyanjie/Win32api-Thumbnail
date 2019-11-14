#ifndef WIDGET_H
#define WIDGET_H

#include <QWidget>
#include <windows.h>

class QPushButton;

class Widget : public QWidget
{
    Q_OBJECT

public:
    Widget(QWidget *parent = 0);
    ~Widget();

private:
    static BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam);
    HRESULT drawThumbnail(HWND hwnd, LPCWCH winText, RECT desRect);

    QList<QWidget*> m_buttons;
    static QStringList s_windowTitleList;
};

#endif // WIDGET_H
