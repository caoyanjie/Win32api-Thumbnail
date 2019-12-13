#ifndef WIDGET_H
#define WIDGET_H

#include <QWidget>
#include <windows.h>
#include <dwmapi.h>

//#include <d3d11.h>
//#include <dxgi1_2.h>

class QPushButton;
class QLabel;

class Widget : public QWidget
{
    Q_OBJECT

public:
    Widget(QWidget *parent = 0);
    ~Widget();

private:
    static BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam);
    HRESULT drawThumbnailByHwnd(HWND destHwnd, HWND srcHwnd, RECT desRect);
    HRESULT drawThumbnailByWintext(HWND hwnd, LPCWCH winText, RECT desRect);

    QList<QWidget*> 	m_buttons;
    QList<QLabel*>		m_labels;
    QList<HTHUMBNAIL>   m_thumbnails;
    QWidget*			m_widget;

    static QList<HWND>	s_hwnds;
    static QStringList 	s_windowTitleList;

    //ID3D11Device* d3d11Device = nullptr;
    //IDXGIOutput* DxgiOutput;
    //IDXGIOutputDuplication* m_DeskDupl = nullptr;

    void updateThumbnail();

private slots:
    void timeout();
};

#endif // WIDGET_H
