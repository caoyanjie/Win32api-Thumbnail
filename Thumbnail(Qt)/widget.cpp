#include "widget.h"
#include <qt_windows.h>
#include <QtWinExtras/qwinfunctions.h>
#include <QtWinExtras/qtwinextrasversion.h>
#include <QtWinExtras/qwinextrasglobal.h>
#include <QtWinExtras/qwintaskbarbutton.h>
#include <tchar.h>

#include <QLabel>
#include <QHBoxLayout>
#include <QGridLayout>
#include <QDebug>
#include <QPushButton>
#include <QScrollArea>
#include <QTimer>
#include <QPixmap>
#include <QApplication>
#include <QScreen>
#include <QDesktopWidget>
#include <QWindow>


QStringList Widget::s_windowTitleList;
QList<HWND> Widget::s_hwnds;

const int ItemWidth = 300;
const int ItemHeight = 200;
const int Padding = 10;
const int BlockWidth = ItemWidth + Padding;
const int BlockHeight = ItemHeight + Padding;

Widget::Widget(QWidget *parent)
    : QWidget(parent)
{
    //#define __T(x) L##x

    this->resize(1600, 800);

    QScrollArea *scrollArea = new QScrollArea(this);
    scrollArea->resize(this->size());
    m_widget = new QWidget(scrollArea);
    scrollArea->setWidget(m_widget);
    m_widget->resize(scrollArea->size());

    QGridLayout *layout = new QGridLayout();
    m_widget->setLayout(layout);

    QTimer *timer = new QTimer(this);
    connect(timer, SIGNAL(timeout()), this, SLOT(timeout()));
    timer->start(10);

    // 添加一个特殊窗口，实时显示桌面上的内容，因为通过EnumWindows()函数枚举出来的桌面句柄，只能得到桌面壁纸的缩略图
    HWND _desktopHandle = (HWND)QApplication::desktop()->winId();
    s_hwnds.append(_desktopHandle);

    // 枚举所有窗口
    EnumWindows(&Widget::EnumWindowsProc, 0);

    const int ItemsOfLine = this->width() / BlockWidth;
    for (int i = 0; i < s_hwnds.size(); ++i)
    {
        int line = i / ItemsOfLine;
        int column = i % ItemsOfLine;

        m_widget->setFixedHeight((line+1)*BlockHeight);

        QLabel* lab = new QLabel();
        lab->setFixedSize(ItemWidth, ItemHeight);
        layout->addWidget(lab, line, column, 1, 1);
        m_labels.append(lab);

        if (i == 0)
        {
        }
        else
        {
            RECT _rect = {column*BlockWidth, line*BlockHeight, column*BlockWidth+ItemWidth, line*BlockHeight+ItemHeight};
            drawThumbnailByHwnd((HWND)this->winId(), s_hwnds.at(i), _rect);
        }
    }
}

Widget::~Widget()
{
}

// 枚举所有窗口
//BOOL Widget::EnumWindowsProc(HWND hwnd, LPARAM lParam)
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
        s_hwnds.append(hwnd);
        s_windowTitleList.append(QString::fromLocal8Bit(lpWindowName));
    }

    //EnumChildWindows(hwnd, EnumChildProc, lParam);

    return TRUE;
}

// 通过窗口句柄绘制缩略图
HRESULT Widget::drawThumbnailByHwnd(HWND destHwnd, HWND srcHwnd, RECT desRect)
{
    HRESULT hr = S_OK;
    HTHUMBNAIL thumbnail = NULL;
    hr = DwmRegisterThumbnail(destHwnd, srcHwnd, &thumbnail);

    if (SUCCEEDED(hr) && NULL != thumbnail)
    {
        // 设置绘制属性
        DWM_THUMBNAIL_PROPERTIES dskThumbProps;
        dskThumbProps.dwFlags = DWM_TNP_SOURCECLIENTAREAONLY | DWM_TNP_VISIBLE | DWM_TNP_OPACITY | DWM_TNP_RECTDESTINATION;
        dskThumbProps.fSourceClientAreaOnly = FALSE;
        dskThumbProps.fVisible = TRUE;
        dskThumbProps.opacity = 255;//(255 * 70)/100;

        // 获取缩略图尺寸
        SIZE pSize;
        DwmQueryThumbnailSourceSize(thumbnail, &pSize);

        // 计算绘制位置
        dskThumbProps.rcDestination = desRect;
        //dskThumbProps.rcSource.left = 80;
        //dskThumbProps.rcSource.top = 80;
        //dskThumbProps.rcSource.right = 130;
        //dskThumbProps.rcSource.bottom = 130;

        // 更新缩略图
        hr = DwmUpdateThumbnailProperties(thumbnail, &dskThumbProps);

        m_thumbnails.append(thumbnail);

        ///qDebug() << (SUCCEEDED(hr) ? "update" : "update error");
    }
    else
    {
        qDebug() << "注册缩略图失败: " << srcHwnd;
    }

    return hr;
}

// 通过窗口标题绘制缩略图
HRESULT Widget::drawThumbnailByWintext(HWND hwnd, /*LPCSTR*/ LPCWCH /*WCHAR*/ winText, RECT desRect)
{
    //qDebug() << winText;

    HRESULT hr = S_OK;
    HTHUMBNAIL thumbnail = NULL;

    // Register the thumbnail
    hr = DwmRegisterThumbnail(hwnd, FindWindow(NULL,  winText), &thumbnail);
    // hr = DwmRegisterThumbnail(hwnd, FindWindow(L"Progman", NULL), &thumbnail);  					// 桌面 ok，但是如果刚好有一个窗口的类名为 Progman 获得的就是那个窗口(没经过测试）
    // hr = DwmRegisterThumbnail(hwnd, FindWindow(NULL, L"Program Manager"), &thumbnail);			// 桌面 ok，但是如果刚好有一个窗口标题也叫 Program Manager 获得的就是那个窗口
    // hr = DwmRegisterThumbnail(hwnd, FindWindow(L"Progman", L"Program Manager"), &thumbnail); 	// 桌面 ok
    // hr = DwmRegisterThumbnail(hwnd, FindWindow(L"Shell_TrayWnd",  NULL), &thumbnail);			// 任务栏 ok
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

// 更新缩略图
void Widget::updateThumbnail()
{
    for (int i = 0; i < m_labels.size(); ++i)
    {
        if (m_thumbnails.size()-1 < i)
        {
            return;
        }

        if (i == 0)
        {
            continue;
        }

        QLabel *lab = m_labels.at(i);
        HTHUMBNAIL thumbnail = m_thumbnails.at(i);

        // 设置绘制属性
        DWM_THUMBNAIL_PROPERTIES dskThumbProps;
        dskThumbProps.dwFlags = DWM_TNP_SOURCECLIENTAREAONLY | DWM_TNP_VISIBLE | DWM_TNP_OPACITY | DWM_TNP_RECTDESTINATION;
        dskThumbProps.fSourceClientAreaOnly = FALSE;
        dskThumbProps.fVisible = TRUE;
        dskThumbProps.opacity = 255;//(255 * 70)/100;

        // 获取缩略图尺寸
        SIZE pSize;
        DwmQueryThumbnailSourceSize(thumbnail, &pSize);

        // 计算绘制位置
        QPoint point = m_widget->mapTo(this, lab->pos());
        //RECT rect = {point.rx(), point.ry(), point.rx()+ItemWidth, point.ry()+ItemHeight};
        float rate = (float)pSize.cx / pSize.cy;
        int relHeight = pSize.cx/rate;
        if (relHeight > 500)
            relHeight = 200;
        RECT rect = {point.rx(), point.ry(), point.rx()+ItemWidth, point.ry()+relHeight};
        dskThumbProps.rcDestination = rect;

        // 更新缩略图
        HRESULT hr = S_OK;
        hr = DwmUpdateThumbnailProperties(thumbnail, &dskThumbProps);
        //qDebug() << (SUCCEEDED(hr) ? "更新成功" : "更新失败");
    }

}

void Widget::timeout()
{
    QWindow *window = windowHandle();
    QScreen *screen = window->screen();
    QPixmap pic = screen->grabWindow(QApplication::desktop()->winId(), 0, 0, screen->size().width(), screen->size().height());
    QPixmap scaledPixmap = pic.scaled(m_labels.at(0)->size(), Qt::KeepAspectRatio);
    m_labels.at(0)->setPixmap(scaledPixmap);

    QPoint point = m_widget->mapTo(this, m_labels.at(0)->pos());
    static QPoint lastPoint = point;
    if (point != lastPoint)
    {
        updateThumbnail();
    }

    lastPoint = point;
}
