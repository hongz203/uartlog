# UartLogTerminal (WPF, .NET 8)

Windows UART 로그를 받아 Raw/Filtered 두 창으로 보는 MVP 프로젝트입니다.

## Screenshots

### Main Window

![Main Window](docs/images/main-window.png)

> 스크린샷 파일 경로: `docs/images/main-window.png`

## Prerequisites

- Visual Studio 2022 Community 이상
- Workload: `.NET desktop development`

## Open

1. `UartLogTerminal/UartLogTerminal.sln` 열기
2. NuGet restore 실행 (자동)
3. `F5` 실행

## Quick Usage

1. 상단에서 `COM Port` 선택 후 옆의 `Refresh` 아이콘으로 포트 목록 갱신
2. `Baud`를 선택하고 `Connect`(plug 아이콘) 클릭
3. 필요하면 `Baud` 옆 톱니바퀴 아이콘으로 `Advanced Serial Settings`를 펼쳐 `Data bits / Parity / Stop bits / Flow control / DTR / RTS` 설정
4. 상단 `Filters` 아이콘들로 필터 탭 추가/삭제 및 패널 도킹 방향 전환
5. `TX` 입력창에 명령을 입력하고 `Enter` 또는 `Send`
6. `Open Log File`(folder-open 아이콘)로 저장된 로그 파일(`.log`, `.txt`)을 로드해 post-mortem 분석
7. 테마 아이콘 버튼으로 Dark/Light 전환
8. 각 탭에서 `Keyword / Regex`, `Match case` 설정
9. 각 탭에서 `FG`, `BG` 색상을 골라 키워드 하이라이트 확인
10. Filtered 창에서 마우스로 원하는 구간을 드래그 선택 후 `Ctrl+C`로 복사
11. 필요 시 `Paused` 또는 `Clear All` 사용

## Current Features (MVP)

- COM 포트 목록 조회 및 선택
- Baud rate 선택
- Advanced Serial Settings
  - Data bits / Parity / Stop bits / Flow control(Handshake)
  - DTR / RTS 토글
- Connect / Disconnect
- Dark / Light 테마 전환
- VS Code 스타일의 플랫 UI(테두리 최소화, tone 기반 구분)
- Raw 로그 창
  - UART 실시간 수신 시 `Live` + 녹색 점 표시
  - 파일 로드 시 파일명 표시
- 필터 탭 다중화
  - 탭별 키워드/Regex/대소문자 설정
  - 탭별 Foreground/Background 색상 설정
  - 매칭된 키워드 구간만 색상 하이라이트
  - 상단 아이콘으로 필터 탭 추가/삭제 및 패널 도킹 전환(Right/Bottom)
  - Filtered 로그는 RichTextBox 기반으로 문자 단위 드래그 선택/복사 지원
- TX 콘솔 (라인 송신, Enter 전송)
- 저장 로그 파일 로드 (post-mortem 필터링)
- Pause / Clear
- Timestamp 표시
- VS Code 스타일 오버레이 스크롤바 (hover 시 표시)

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

## Notes

- 스크린샷을 추가하려면 앱 실행 후 화면 캡처해서 `docs/images/main-window.png`로 저장하세요.
- 저장소에 `docs/images/` 폴더가 없으면 먼저 생성하세요.
- 샘플 로그는 `sample-logs/quick-kernel.log`, `sample-logs/linux-kernel-target.log`를 사용하세요.

## Next Extensions

- 컬러 하이라이트 룰
- 로그 파일 저장/회전 저장
- TX 콘솔 (사용자 입력 송신)
