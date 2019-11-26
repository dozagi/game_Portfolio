// Fill out your copyright notice in the Description page of Project Settings.

#include "BTTask_WanderForTarget.h"

#include "Monster/Dog/Dog.h"
#include "Monster/Dog/DogAIController.h"
#include "MyCharacter/MotionControllerCharacter.h"
#include "BehaviorTree/BlackboardComponent.h"
#include "GameFramework/CharacterMovementComponent.h"
#include "Kismet/KismetMathLibrary.h"
#include "Camera/CameraComponent.h"

// 개 몬스터가 플레이어 주위를 돌기 위한 코드입니다

EBTNodeResult::Type UBTTask_WanderForTarget::ExecuteTask(UBehaviorTreeComponent & OwnerComp, uint8 * NodeMemory)
{
	Super::ExecuteTask(OwnerComp, NodeMemory);

	ADogAIController* AI = Cast<ADogAIController>(OwnerComp.GetAIOwner());

	if (AI)
	{
		AActor* Player = Cast<AActor>(AI->BBComponent->GetValueAsObject(TEXT("Player")));
		AMotionControllerCharacter* MyCharacter = Cast<AMotionControllerCharacter>(Player);
		ADog* RagdollDog = Cast<ADog>(AI->GetPawn());

		int Rotcheck;

		if (RagdollDog && MyCharacter)
		{
			if (!RagdollDog->AttachActor)
			{
				// 개가 플레이어의 정면을 보기위해 최단 경로를 찾습니다.
				Rotcheck = AI->BBComponent->GetValueAsInt("RotateCheck");

				if (Rotcheck == 1)
				{
					time = 1.0f;
				}
				else if (Rotcheck == 2)
				{
					time = -1.0f;
				}

				// time값에 따라 개가 플레이어를 바라보며 회전하게 합니다. 
				// RotateAngleAxis : FVector(0.0f, 0.0f, 1.0f)을 기준으로 time값 만큼 회전된 값을 반환합니다. 그 값을 개의 위치로 합니다. 결국 플레이어 주위를 돌아갑니다.。
				// RotatorFromAxisAndAngle : FVector(0.0f, 0.0f, 1.0f)축을 기준으로 time값 만큼 회전값을 없습니다. 그것을 개의 Rotation로 합니다.
				FVector Vec1 = UKismetMathLibrary::Subtract_VectorVector(RagdollDog->GetActorLocation(), MyCharacter->Camera->GetComponentLocation());
				FVector Vec2 = UKismetMathLibrary::RotateAngleAxis(Vec1, time, FVector(0.0f, 0.0f, 1.0f));
				
				FVector Vec3 = MyCharacter->Camera->GetComponentLocation() + Vec2;

				FRotator Rot1 = RagdollDog->GetActorRotation() + UKismetMathLibrary::RotatorFromAxisAndAngle(FVector(0.0f, 0.0f, 1.0f), time);

				RagdollDog->SetActorLocationAndRotation(Vec3, Rot1);
				return EBTNodeResult::Succeeded;
			}
		}
	}

	return EBTNodeResult::Failed;
}

void UBTTask_WanderForTarget::InitializeFromAsset(UBehaviorTree & Asset)
{
	Super::InitializeFromAsset(Asset);

	time = 0.0f;
}
