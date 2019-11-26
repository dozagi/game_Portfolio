// Fill out your copyright notice in the Description page of Project Settings.

#include "PlayerSword.h"
#include "Components/StaticMeshComponent.h"
#include "UObject/ConstructorHelpers.h"
#include "Components/CapsuleComponent.h"
#include "Kismet/GameplayStatics.h"

#include "MyCharacter/MotionControllerCharacter.h"

#include "Engine/StaticMesh.h"

#include "Haptics/HapticFeedbackEffect_Base.h"
#include "MyCharacter/MotionControllerPC.h"
#include "HandMotionController/RightHandMotionController.h"
#include "Monster/Dog/Dog.h"

// 플레이어의 검입니다.

// Sets default values
APlayerSword::APlayerSword()
{
	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	SwordMesh = CreateDefaultSubobject<UStaticMeshComponent>(TEXT("SwordMesh"));
	SetRootComponent(SwordMesh);
	SwordMesh->SetCollisionProfileName(TEXT("NoCollision"));

	static ConstructorHelpers::FObjectFinder<UStaticMesh>SM_Sword(TEXT("StaticMesh'/Game/Assets/CharacterEquipment/Equipment/Sword/Mesh/SM_Sword.SM_Sword'"));
	if (SM_Sword.Succeeded())
	{
		SwordMesh->SetStaticMesh(SM_Sword.Object);
	}

	SwordCollision = CreateDefaultSubobject<UCapsuleComponent>(TEXT("SwordCollision"));
	SwordCollision->SetupAttachment(SwordMesh);	
	SwordCollision->SetCollisionProfileName(TEXT("OverlapAll"));

	SwordCollision->SetRelativeLocation(FVector(0.0f, 90.0f, 0.0f));
	SwordCollision->SetRelativeRotation(FRotator(0.0f, 0.0f, 90.0f));
	SwordCollision->SetRelativeScale3D(FVector(0.75f, 0.75f, 1.6f));
	SwordCollision->ComponentTags.Add(FName(TEXT("PlayerSwordCollision")));

	Timer = 0.0f;		// 데미지 중첩을 막기위해 타이머를 주었습니다.

	// 태그
	Tags.Add(FName(TEXT("DisregardForLeftHand")));
	Tags.Add(FName(TEXT("DisregardForRightHand")));
}

// Called when the game starts or when spawned
void APlayerSword::BeginPlay()
{
	Super::BeginPlay();

	// 오너 설정
	SwordOwner = Cast<AMotionControllerCharacter>(UGameplayStatics::GetPlayerCharacter(GetWorld(), 0));

	// 공격
	if (SwordCollision)
	{
		SwordCollision->OnComponentBeginOverlap.AddDynamic(this, &APlayerSword::OnSwordOverlap);
	}
}

// Called every frame
void APlayerSword::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	Timer += DeltaTime;		// 타이머

	// 속도롤 찾습니다.
	if (SwordOwner)
	{
		SwordCurrentPosistion = SwordCollision->GetComponentLocation() - GetActorLocation();
		SwordMoveDelta = SwordCurrentPosistion - SwordPreviousPosistion;
		SwordMoveVelocity = SwordMoveDelta / DeltaTime;
		SwordPreviousPosistion = SwordCurrentPosistion;
	}
}

void APlayerSword::OnSwordOverlap(UPrimitiveComponent * OverlappedComp, AActor * OtherActor, UPrimitiveComponent * OtherComp,
	int32 OtherBodyIndex, bool bFromSweep, const FHitResult & SweepResult)
{
	// 몬스터 액터와 오버랩 됐을 때
	if (OtherActor->ActorHasTag("Monster"))
	{
		// 타이머로 데미지 중첩을 방지합니다.
		if (Timer >= 0.5f)
		{
			// 일정 속도 이상일 때만 데미지를 줍니다.
			if (SwordMoveVelocity.Size() >= 1500)
			{
				Timer = 0.0f;

				// 속도에 따라 데미지를 다르게 줍니다.
				if (SwordMoveVelocity.Size() <= 2500)
				{
					Damage = 10.0f;
				}
				else
				{
					Damage = 15.0f;
				}

				UGameplayStatics::ApplyDamage(OtherActor, Damage, UGameplayStatics::GetPlayerController(GetWorld(), 0), this, nullptr);
			}
		}
	}
}

// 공격을 위해 꽉 쥐고 있는 상태가 아니면 투명도를 주고 있습니다.
void APlayerSword::ConvertOfOpacity(float opacity)
{
	if (SwordMesh)
	{
		SwordMesh->SetScalarParameterValueOnMaterials(FName(TEXT("SwordOpacity")), opacity);
	}
}

