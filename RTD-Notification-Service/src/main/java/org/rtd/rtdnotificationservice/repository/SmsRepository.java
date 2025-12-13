package org.rtd.rtdnotificationservice.repository;

import org.rtd.rtdnotificationservice.entity.Sms;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.stereotype.Repository;

@Repository
public interface SmsRepository extends JpaRepository<Sms, Long> {
}
