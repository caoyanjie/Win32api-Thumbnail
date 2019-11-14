#include "widget.h"
#include <windows.h>
#include <qt_windows.h>
#include <QtWinExtras/qwinfunctions.h>
#include <QtWinExtras/qtwinextrasversion.h>
#include <QtWinExtras/qwinextrasglobal.h>
#include <QtWinExtras/qwintaskbarbutton.h>
#include <dwmapi.h>
#include <tchar.h>

#include <QLabel>
#include <QHBoxLayout>
#include <QDebug>
#include <QPushButton>

QStringList Widget::s_windowTitleList;

Widget::Widget(QWidget *parent)
    : QWidget(parent)
{
    //#define __T(x) L##x

    this->resize(3500, 800);
    EnumWindows(&Widget::EnumWindowsProc, 0);

    //QHBoxLayout* _layout = new QHBoxLayout();
    for (int i = 0; i < s_windowTitleList.size(); ++i)
    {
        RECT _rect = {i*310, 0, i*310+300, 200};
        drawThumbnail((HWND)this->winId(), s_windowTitleList.at(i).toStdWString().c_str(), _rect);

        //QLabel* _label = new QLabel();
        //_label->setStyleSheet("background: rgba(255, 0, 0, 100)");
        //_label->raise();
        //_layout->addWidget(_label);
    }
    //this->setLayout(_layout);
}

Widget::~Widget()
{
}

// 枚举所有窗口
WINBOOL Widget::EnumWindowsProc(HWND hwnd, LPARAM lParam)
{
    char szTitle[MAX_PATH] = { 0 };
    char szClass[MAX_PATH] = { 0 };
    WCHAR szwTtile[MAX_PATH] = { 0 };
    int nMaxCount = MAX_PATH;

    LPSTR lpClassName = szClass;
    LPSTR lpWindowName = szTitle;
    //LPWSTR lpwWindowName = szwTtile;

    //GetWindowText(hwnd, lpwWindowName, nMaxCount);
    GetWindowTextA(hwnd, lpWindowName, nMaxCount);
    GetClassNameA(hwnd, lpClassName, nMaxCount);
    if (*lpWindowName != '\0' && IsWindow(hwnd) && IsWindowEnabled(hwnd) && IsWindowVisible(hwnd) /*&& IsWindowUnicode(hwnd)*/ && strcmp(lpClassName, "Windows.UI.Core.CoreWindow") != 0 && strcmp(lpClassName, "ApplicationFrameWindow") != 0 && strcmp(lpClassName, "Progman") != 0 )
    {
        s_windowTitleList.append(QString::fromLocal8Bit(lpWindowName));
    }

    //EnumChildWindows(hwnd, EnumChildProc, lParam);

    return TRUE;
}

// 绘制缩略图
HRESULT Widget::drawThumbnail(HWND hwnd, /*LPCSTR*/ LPCWCH /*WCHAR*/ winText, RECT desRect)
{
    //qDebug() << winText;

    HRESULT hr = S_OK;
    HTHUMBNAIL thumbnail = NULL;

    // Register the thumbnail
    hr = DwmRegisterThumbnail(hwnd, FindWindow(NULL,  winText), &thumbnail);
    if (SUCCEEDED(hr))
    {
        //qDebug() << "draw: " << winText;
        // Specify the destination rectangle size

        //SIZE pSize;
        //DwmQueryThumbnailSourceSize(thumbnail, &pSize);

        //RECT dest = {0,50,pSize.cx,pSize.cy};
        //RECT dest = {0,50,400,1000};

        // Set the thumbnail properties for use
        DWM_THUMBNAIL_PROPERTIES dskThumbProps;
        dskThumbProps.dwFlags = DWM_TNP_SOURCECLIENTAREAONLY | DWM_TNP_VISIBLE | DWM_TNP_OPACITY | DWM_TNP_RECTDESTINATION;
        dskThumbProps.fSourceClientAreaOnly = FALSE;
        dskThumbProps.fVisible = TRUE;
        dskThumbProps.opacity = 255;//(255 * 70)/100;
        //dskThumbProps.rcDestination = dest;
        dskThumbProps.rcDestination = desRect;

        // Display the thumbnail
        hr = DwmUpdateThumbnailProperties(thumbnail, &dskThumbProps);
        if (SUCCEEDED(hr))
        {
            // ...
            qDebug() << "update";
        }
        else
        {
            qDebug() << "update error";
        }
    }
    else
    {
        qDebug() << "xxxerror: " << winText;
    }

    return hr;
}
