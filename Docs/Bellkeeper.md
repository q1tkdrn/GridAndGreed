# 장송의 종지기 — 종루

왕국이 죽음을 거부한 뒤에도 장례의 종을 울리는 마지막 종지기. 보내지 못한 영혼들이 종 안에 쌓여, 이제는 자신의 장례조차 끝내지 못한다.

> 첫 번째는 산 자를 위해. 두 번째는 죽은 자를 위해. 세 번째는… 너를 위해.

## 전투

- HP 60 / 의지력 4 / 패턴 피해 4 / 공격 예고 1.2초.
- **청동 수의**: 1·2번째 턴에는 받는 모든 양수 피해 -2, 최소 1. 0 피해는 그대로 0이다.
- **세 번째 종**: 3·6·9…번째 턴에는 피해 감소가 사라지고 받는 심판 피해가 3 증가한다. 힘과 기타 피해는 정상 적용한다.
- 세 패턴 묶음 중 하나를 추첨해 반드시 세 턴 동안 순서대로 실행한다. 세 번째 공격과 약점 노출이 항상 맞물린다.
- 기존 50%·30% HP 하한과 묶음 종료 시 해제 규칙을 따른다.

| 묶음 | 첫 번째 종 | 두 번째 종 | 세 번째 종 |
| --- | --- | --- | --- |
| 잔향 | 보드 바깥 두 겹 | 중앙 5×5의 테두리 | 중앙 3×3 |
| 진자 | 왼쪽 세 열 | 오른쪽 세 열 | 가운데 세 열 |
| 장송 | 폭 3칸 주대각선 | 폭 3칸 반대각선 | 중앙 십자 |

공략: 첫 두 턴에는 안전한 칸으로 이동하며 심판 기회를 준비한다. 세 번째 턴에는 공격 예고를 피해 심판을 집중한다. 이동과 심판에 쓸 행동력을 함께 관리하게 하는 것이 설계 의도다. 수치는 초기안이며 실플레이 난이도 조정이 필요하다.

## 게임에서 확인

정식 빌드에 포함된 Assets/Sanggu/Scenes/BattleTemp.unity의 여정 후보에 **종루**를 추가했다. 기존 무작위 경로 선택에 등장한다.

빠른 확인: 같은 씬의 BattleDisplayManager 컴포넌트에서 **Debug Start Stage → Bellkeeper**를 선택하고 Play. 기본값 Normal은 그대로 유지했다. 전투마다 현재 타종 단계와 피해 규칙이 텍스트로 안내된다.

전용 보스 스프라이트와 경로 선택 이미지는 Bellkeeper.png를 사용한다. 음악은 프로젝트의 기존 성당 인트로·루프를 재사용한다. 개별 업적은 추가하지 않았으며 기존 보스 처치 공통 업적과 보상을 따른다.

## 파일과 검증

- 보스 데이터: Assets/Sanggu/ScriptableObjects/Boss/Bellkeeper.asset
- 일러스트: Assets/Sanggu/Resources/Sprites/Boss/Bellkeeper.png (1024×1536, 알파 포함)
- 패시브: BossPassiveState.cs / 턴 안내: BoardPanel.cs
- 패턴: Plate.cs의 BellkeeperSequence / 연결: BattleManagerTemp.cs
- pwsh -NoProfile -File Tests/RunBossPassiveTests.ps1: 패시브 282, 저승 127, HP 하한 47개 검사 통과.
- pwsh -NoProfile -File Tests/RunPatternTests.ps1: 좌표·순서 4,825개 검사 통과. 신규 9개 패턴 전체 좌표와 묶음 유지·초기화 검증 포함.
- Unity 에디터 회귀 검사에 신규 에셋, 이미지, 음악, 여정 등록, 전용 패턴 연결 검사를 추가했다. Unity 배치 실행은 라이선스 초기화 실패로 회귀 검사에 도달하지 못해 중단했다. 전체 컴파일·에셋 임포트·실플레이는 미검증이다. 별도 dotnet 빌드도 환경의 NuGet.Config 접근 제한으로 완료하지 못했다.

## 일러스트 생성 기록

내장 image_gen 도구로 생성했고 생성된 PNG의 알파를 그대로 보존했다. 최종 프롬프트:

Use case: stylized-concept. Asset type: transparent portrait boss sprite for GridAndGreed, a somber Korean 2D tactical game about death with monochrome ink character art. Create a new original boss 'The Funeral Bell Keeper': an unnaturally tall, thin faceless humanoid in torn ceremonial robes, its head hidden inside a cracked antique church bell, a circular iron halo attached behind the bell, long skeletal gloved hands holding the two ends of a heavy bell chain diagonally across the body, small funeral bells suspended at its waist. One solitary character, three-quarter frontal view, complete silhouette from halo to ragged hem, centered with margins. Hand-drawn harsh dry-brush ink, angular woodcut shapes, sparse stipple texture, black charcoal grey and bone white only, striking readable silhouette and eerie restraint. No environment, no ground, no writing or numbers, no labels, no logo, no watermark, no frame, no extra characters. Actual transparent background. Portrait aspect ratio. Keep fine details subordinate to bold large shapes so this is readable in a small game UI.
