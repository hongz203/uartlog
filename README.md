# UartLogTerminal (WPF, .NET 8)

Windows UART 로그를 받아 Raw/Filtered 두 창으로 보는 MVP 프로젝트입니다.

## Prerequisites

- Visual Studio 2022 Community 이상
- Workload: `.NET desktop development`

## Open

1. `UartLogTerminal/UartLogTerminal.sln` 열기
2. NuGet restore 실행 (자동)
3. `F5` 실행

## Current Features (MVP)

- COM 포트 목록 조회 및 선택
- Baud rate 선택
- Connect / Disconnect
- Raw 로그 창
- 필터 탭 다중화
  - 탭별 키워드/Regex/대소문자 설정
  - 탭별 Foreground/Background 색상 설정
  - 매칭된 키워드 구간만 색상 하이라이트
- TX 콘솔 (라인 송신, Enter 전송)
- Pause / Clear
- Timestamp 표시

## Architecture

- `Services/SerialPortService.cs`
  - UART 연결/해제
  - 비동기 수신 chunk를 line 단위로 변환
- `Filtering/FilterEngine.cs`
  - Keyword / Regex 매칭
- `ViewModels/MainViewModel.cs`
  - UI 상태, 명령, Raw/FilterTab 버퍼 관리
- `ViewModels/FilterTabViewModel.cs`
  - 탭별 필터 조건/색상/라인 목록
- `MainWindow.xaml`
  - 컨트롤 레이아웃 및 바인딩

## Next Extensions

- 컬러 하이라이트 룰
- 로그 파일 저장/회전 저장
- TX 콘솔 (사용자 입력 송신)
