매치 3 특수블록 예시

MoleBlock : 턴마다 랜덤하게 자리를 이동하며 같은 색의 기본블록을 주변 매치했을때 제거 가능한 특수블록. 두더지 메타포 사용
- IngameMoleBlockController.cs : 두더지 오브젝트를 관리하는 컨트롤러 스크립트. 두더지들을 랜덤한 자리에 생성하고. 턴이 끝날때마다 우선순위를 계산 후 다른 자리로 이동시킴
- MoleBlock.cs : 두더지 오브젝트의 이동, 제거 등의 연출에 사용

MuffinBlock : 같은 그룹의 모든 접시 블록이 제거될경우 새로운 머핀 블록을 다른자리에 생성하며 사라짐. 머핀 접시 메타포 사용.
- ClocheBlock.cs : 머핀이 담겨있는 접시 오브젝트 연출용
- InGameMuffinBlockController.cs : 모든 관련 오브젝트를 관리하는 컨트롤러 스크립트. 접시 그룹 제거 확인 및 머핀 블록 생성 관리
- MuffinBlock.cs : 클로시 블록 제거 후 생성되는 머핀 오브젝트 연출용

SquirrelBlock : 매 턴마다 랜덤 혹은 세팅된 경로로 한칸씩 이동하는 다람쥐 오브젝트 
- InGameSquirrelBlockController.cs : 다람쥐블록의 이동을 관리, 각 예외상황에서의 다람쥐 움직임 컨트롤
- SquirrelBlock.cs : 다람쥐 오브젝트의 연출 관리
