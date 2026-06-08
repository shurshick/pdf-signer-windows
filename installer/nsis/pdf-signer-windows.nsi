Unicode true
RequestExecutionLevel user

!ifndef VERSION
  !define VERSION "0.1.0"
!endif

!define APP_NAME "PDF Signer Windows"
!define COMPANY "shurshick"
!define APP_EXE "PdfSignerWindows.exe"
!define SOURCE_DIR "..\..\artifacts\portable"

Name "${APP_NAME}"
OutFile "..\..\artifacts\pdf-signer-windows-${VERSION}-setup.exe"
InstallDir "$LOCALAPPDATA\${APP_NAME}"
InstallDirRegKey HKCU "Software\${COMPANY}\${APP_NAME}" "InstallDir"

!include MUI2.nsh

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "Russian"

Section "Install"
  SetOutPath "$INSTDIR"
  File /r "${SOURCE_DIR}\*.*"
  WriteRegStr HKCU "Software\${COMPANY}\${APP_NAME}" "InstallDir" "$INSTDIR"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk" "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Uninstall"
  Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk"
  RMDir "$SMPROGRAMS\${APP_NAME}"

  Delete "$INSTDIR\${APP_EXE}"
  Delete "$INSTDIR\*.dll"
  Delete "$INSTDIR\*.config"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"

  DeleteRegKey HKCU "Software\${COMPANY}\${APP_NAME}"
SectionEnd
