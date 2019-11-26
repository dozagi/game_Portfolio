// Fill out your copyright notice in the Description page of Project Settings.

#include "CustomWidget_1.h"
#include "Widgets/CustomButton.h"
#include "Kismet/GameplayStatics.h"
#include "MyCharacter/MotionControllerCharacter.h"

void UCustomWidget_1::NativeConstruct()
{
	Super::NativeConstruct();

	// 위젯 내의 이름을 캐스트 합니다
	CB_1 = Cast<UCustomButton>(GetWidgetFromName(TEXT("ExitButton")));
	CB_2 = Cast<UCustomButton>(GetWidgetFromName(TEXT("CancelButton")));

	// 캐스트 된 각 버튼을 터치 -> 그랩할 경우에 발생할 이벤트를 설정합니다.
	CB_1->OnClicked.AddDynamic(this, &UCustomWidget_1::OnClickedCB_1);
	CB_2->OnClicked.AddDynamic(this, &UCustomWidget_1::OnClickedCB_2);
}

// 게임 종료. 게임 시작 전의 초기의 위치로 돌아갑니다.
void UCustomWidget_1::OnClickedCB_1()
{
	Cast<AMotionControllerCharacter>(UGameplayStatics::GetPlayerCharacter(GetWorld(), 0))->MoveMainScene();
}

// 게임 종료를 위한 메뉴 창을 생성합니다.
void UCustomWidget_1::OnClickedCB_2()
{
	Cast<AMotionControllerCharacter>(UGameplayStatics::GetPlayerCharacter(GetWorld(), 0))->GameMenu();
}